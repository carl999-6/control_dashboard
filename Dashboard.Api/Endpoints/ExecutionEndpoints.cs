using System.Collections.Concurrent;
using Dashboard.Api.Contracts;
using Dashboard.Api.Data;
using Dashboard.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Endpoints;

public static class ExecutionEndpoints
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> ProjectLocks = new();
    private static readonly HashSet<string> CountedStatuses = ["awaiting_approval", "approved", "running", "succeeded", "failed"];

    public static IEndpointRouteBuilder MapExecutionEndpoints(this IEndpointRouteBuilder app)
    {
        var operations = app.MapGroup("/api/projects/{projectId:guid}/operations").RequireAuthorization();

        operations.MapGet("/rates", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var items = (await db.ApiRatePlans.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(ct))
                .OrderByDescending(item => item.EffectiveFrom).Select(RatePlanResponse.FromEntity);
            return Results.Ok(items);
        });

        operations.MapPost("/rates", async (Guid projectId, RatePlanRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var errors = ValidateRate(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var provider = request.Provider.Trim().ToLowerInvariant();
            var model = request.Model.Trim().ToLowerInvariant();
            var current = await db.ApiRatePlans.Where(item => item.ProjectId == projectId && item.Provider == provider && item.Model == model && item.IsActive).ToListAsync(ct);
            foreach (var item in current) item.IsActive = false;
            var rate = new ApiRatePlan
            {
                Id = Guid.NewGuid(), ProjectId = projectId, Provider = provider, Model = model,
                InputUsdPerMillion = request.InputUsdPerMillion, OutputUsdPerMillion = request.OutputUsdPerMillion,
                EffectiveFrom = request.EffectiveFrom ?? DateTimeOffset.UtcNow, IsActive = true,
                IsDemoData = false, CreatedAt = DateTimeOffset.UtcNow,
            };
            db.ApiRatePlans.Add(rate);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{projectId}/operations/rates/{rate.Id}", RatePlanResponse.FromEntity(rate));
        });

        operations.MapGet("/budget", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var budget = await GetOrCreateBudget(projectId, db, ct);
            return Results.Ok(BudgetResponse.FromEntity(budget));
        });

        operations.MapPut("/budget", async (Guid projectId, BudgetRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var errors = ValidateBudget(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var budget = await GetOrCreateBudget(projectId, db, ct);
            budget.DailyLimitUsd = request.DailyLimitUsd; budget.MonthlyLimitUsd = request.MonthlyLimitUsd;
            budget.WarningPercent = request.WarningPercent; budget.ExchangeRateGtqPerUsd = request.ExchangeRateGtqPerUsd;
            budget.IsPaused = request.IsPaused; budget.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(BudgetResponse.FromEntity(budget));
        });

        operations.MapGet("/executions", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var items = (await db.Executions.AsNoTracking().Include(item => item.Audit)
                    .Where(item => item.ProjectId == projectId).ToListAsync(ct))
                .OrderByDescending(item => item.CreatedAt).Take(100).Select(ExecutionResponse.FromEntity);
            return Results.Ok(items);
        });

        operations.MapPost("/executions/plan", async (Guid projectId, PlanExecutionRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var errors = ValidatePlan(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var gate = ProjectLocks.GetOrAdd(projectId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(ct);
            try
            {
                var key = request.IdempotencyKey.Trim();
                var existing = await db.Executions.Include(item => item.Audit)
                    .SingleOrDefaultAsync(item => item.ProjectId == projectId && item.IdempotencyKey == key, ct);
                if (existing is not null) return Results.Ok(ExecutionResponse.FromEntity(existing));

                var rate = await FindRate(projectId, request.Provider, request.Model, db, ct);
                if (rate is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["model"] = ["No existe una tarifa activa para este proveedor y modelo."] });
                var budget = await GetOrCreateBudget(projectId, db, ct);
                var costUsd = CalculateCost(request.InputUnits, request.OutputUnits, rate);
                var status = await ResolveBudgetStatus(projectId, costUsd, budget, db, ct);
                if (status == "approved" && request.ApprovalRequired) status = "awaiting_approval";
                var now = DateTimeOffset.UtcNow;
                var item = CreateExecution(projectId, request, rate, budget, status, costUsd, now);
                item.Audit.Add(CreateAudit(projectId, item.Id, "planned", string.Empty, status, StatusNote(status), now));
                db.Executions.Add(item);
                await db.SaveChangesAsync(ct);
                return Results.Created($"/api/projects/{projectId}/operations/executions/{item.Id}", ExecutionResponse.FromEntity(item));
            }
            finally { gate.Release(); }
        });

        operations.MapPost("/executions/{executionId:guid}/approve", async (Guid projectId, Guid executionId, ApprovalRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var item = await db.Executions.Include(value => value.Audit).SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Id == executionId, ct);
            if (item is null) return Results.NotFound();
            if (item.Status != "awaiting_approval") return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["La ejecución no está pendiente de aprobación."] });
            var now = DateTimeOffset.UtcNow; var previous = item.Status;
            item.Status = "approved"; item.ApprovedAt = now; item.ApprovedBy = "Administrador local";
            db.ExecutionAudit.Add(CreateAudit(projectId, item.Id, "approved", previous, item.Status, request.Note ?? "Aprobación humana registrada.", now));
            await db.SaveChangesAsync(ct);
            return Results.Ok(ExecutionResponse.FromEntity(item));
        });

        operations.MapPost("/executions/{executionId:guid}/complete", async (Guid projectId, Guid executionId, CompleteExecutionRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var item = await db.Executions.Include(value => value.Audit).SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Id == executionId, ct);
            if (item is null) return Results.NotFound();
            if (item.Status != "approved") return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Solo una ejecución aprobada puede simularse."] });
            var now = DateTimeOffset.UtcNow; var previous = item.Status;
            item.StartedAt = now; item.CompletedAt = now;
            item.Status = request.Succeeded ? "succeeded" : "failed";
            item.ErrorCode = request.Succeeded ? string.Empty : request.ErrorCode?.Trim() ?? "SIMULATED_FAILURE";
            item.ErrorMessage = request.Succeeded ? string.Empty : request.ErrorMessage?.Trim() ?? "Fallo simulado.";
            db.ExecutionAudit.Add(CreateAudit(projectId, item.Id, request.Succeeded ? "completed" : "failed", previous, item.Status,
                request.Succeeded ? "Ejecución simulada completada; no hubo llamada externa." : item.ErrorMessage, now));
            await db.SaveChangesAsync(ct);
            return Results.Ok(ExecutionResponse.FromEntity(item));
        });

        operations.MapPost("/executions/{executionId:guid}/retry", async (Guid projectId, Guid executionId, DashboardDbContext db, CancellationToken ct) =>
        {
            var gate = ProjectLocks.GetOrAdd(projectId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(ct);
            try
            {
                var original = await db.Executions.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == executionId, ct);
                if (original is null) return Results.NotFound();
                if (original.Status != "failed") return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Solo una ejecución fallida puede reintentarse."] });
                var nextAttempt = await db.Executions.CountAsync(item => item.ParentExecutionId == original.Id, ct) + 2;
                var key = $"{original.IdempotencyKey}:retry:{nextAttempt}";
                var existing = await db.Executions.Include(item => item.Audit).SingleOrDefaultAsync(item => item.ProjectId == projectId && item.IdempotencyKey == key, ct);
                if (existing is not null) return Results.Ok(ExecutionResponse.FromEntity(existing));
                var rate = await db.ApiRatePlans.SingleAsync(item => item.Id == original.ApiRatePlanId, ct);
                var budget = await GetOrCreateBudget(projectId, db, ct);
                var status = await ResolveBudgetStatus(projectId, original.EstimatedCostUsd, budget, db, ct);
                if (status == "approved" && original.ApprovalRequired) status = "awaiting_approval";
                var now = DateTimeOffset.UtcNow;
                var retryRequest = new PlanExecutionRequest(key, original.Provider, original.Model, original.Flow,
                    original.InputUnits, original.OutputUnits, original.ApprovalRequired);
                var retry = CreateExecution(projectId, retryRequest, rate, budget, status, original.EstimatedCostUsd, now);
                retry.ParentExecutionId = original.Id; retry.AttemptNumber = nextAttempt;
                retry.Audit.Add(CreateAudit(projectId, retry.Id, "retried", original.Status, status, $"Reintento {nextAttempt} creado desde {original.Id}.", now));
                db.Executions.Add(retry);
                await db.SaveChangesAsync(ct);
                return Results.Created($"/api/projects/{projectId}/operations/executions/{retry.Id}", ExecutionResponse.FromEntity(retry));
            }
            finally { gate.Release(); }
        });

        operations.MapPost("/executions/{executionId:guid}/cancel", async (Guid projectId, Guid executionId, DashboardDbContext db, CancellationToken ct) =>
        {
            var item = await db.Executions.Include(value => value.Audit).SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Id == executionId, ct);
            if (item is null) return Results.NotFound();
            if (item.Status is not ("awaiting_approval" or "approved")) return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Esta ejecución ya no puede cancelarse."] });
            var now = DateTimeOffset.UtcNow; var previous = item.Status; item.Status = "cancelled"; item.CompletedAt = now;
            db.ExecutionAudit.Add(CreateAudit(projectId, item.Id, "cancelled", previous, item.Status, "Cancelación manual registrada.", now));
            await db.SaveChangesAsync(ct);
            return Results.Ok(ExecutionResponse.FromEntity(item));
        });

        operations.MapGet("/summary", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var budget = await GetOrCreateBudget(projectId, db, ct);
            var (today, month) = await GetPeriodBounds(projectId, db, ct);
            var items = await db.Executions.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(ct);
            var counted = items.Where(item => CountedStatuses.Contains(item.Status)).ToList();
            var monthItems = counted.Where(item => item.CreatedAt >= month).ToList();
            var monthUsd = monthItems.Sum(item => item.EstimatedCostUsd);
            var providers = monthItems.GroupBy(item => item.Provider).Select(group => new ProviderCostResponse(
                group.Key, group.Sum(item => item.EstimatedCostUsd), group.Sum(item => item.EstimatedCostGtq), group.Count()))
                .OrderByDescending(item => item.CostUsd).ToList();
            return Results.Ok(new ExecutionSummaryResponse(
                counted.Where(item => item.CreatedAt >= today).Sum(item => item.EstimatedCostUsd), monthUsd,
                monthItems.Sum(item => item.EstimatedCostGtq), budget.MonthlyLimitUsd,
                budget.MonthlyLimitUsd > 0 ? Math.Round(monthUsd / budget.MonthlyLimitUsd * 100, 2) : 0,
                items.Count(item => item.Status == "awaiting_approval"), items.Count(item => item.Status == "blocked"),
                items.Count(item => item.Status == "failed"), items.Count(item => item.Status == "succeeded"),
                budget.IsPaused, providers));
        });

        return app;
    }

    private static async Task<bool> ProjectExists(Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        await db.Projects.AnyAsync(item => item.Id == projectId, ct);

    private static async Task<ProjectBudget> GetOrCreateBudget(Guid projectId, DashboardDbContext db, CancellationToken ct)
    {
        var budget = await db.ProjectBudgets.SingleOrDefaultAsync(item => item.ProjectId == projectId, ct);
        if (budget is not null) return budget;
        budget = new ProjectBudget
        {
            Id = Guid.NewGuid(), ProjectId = projectId, DailyLimitUsd = 5, MonthlyLimitUsd = 50,
            WarningPercent = 80, ExchangeRateGtqPerUsd = 7.75m, IsPaused = false, UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.ProjectBudgets.Add(budget);
        await db.SaveChangesAsync(ct);
        return budget;
    }

    private static async Task<ApiRatePlan?> FindRate(Guid projectId, string provider, string model, DashboardDbContext db, CancellationToken ct)
    {
        var normalizedProvider = provider.Trim().ToLowerInvariant(); var normalizedModel = model.Trim().ToLowerInvariant();
        var rates = await db.ApiRatePlans.Where(item => item.ProjectId == projectId && item.Provider == normalizedProvider && item.Model == normalizedModel && item.IsActive).ToListAsync(ct);
        return rates.OrderByDescending(item => item.EffectiveFrom).FirstOrDefault();
    }

    private static decimal CalculateCost(int input, int output, ApiRatePlan rate) =>
        Math.Round(input / 1_000_000m * rate.InputUsdPerMillion + output / 1_000_000m * rate.OutputUsdPerMillion, 8);

    private static async Task<string> ResolveBudgetStatus(Guid projectId, decimal costUsd, ProjectBudget budget, DashboardDbContext db, CancellationToken ct)
    {
        if (budget.IsPaused) return "blocked";
        var (today, month) = await GetPeriodBounds(projectId, db, ct);
        var items = await db.Executions.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(ct);
        var counted = items.Where(item => CountedStatuses.Contains(item.Status)).ToList();
        var daily = counted.Where(item => item.CreatedAt >= today).Sum(item => item.EstimatedCostUsd);
        var monthly = counted.Where(item => item.CreatedAt >= month).Sum(item => item.EstimatedCostUsd);
        return daily + costUsd > budget.DailyLimitUsd || monthly + costUsd > budget.MonthlyLimitUsd ? "blocked" : "approved";
    }

    private static async Task<(DateTimeOffset TodayUtc, DateTimeOffset MonthUtc)> GetPeriodBounds(
        Guid projectId, DashboardDbContext db, CancellationToken ct)
    {
        var zoneId = await db.Projects.AsNoTracking().Where(item => item.Id == projectId)
            .Select(item => item.TimeZone).SingleAsync(ct);
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId); }
        catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { zone = TimeZoneInfo.Utc; }

        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
        var todayLocal = DateTime.SpecifyKind(localNow.Date, DateTimeKind.Unspecified);
        var monthLocal = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return (new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(todayLocal, zone)),
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(monthLocal, zone)));
    }

    private static ExecutionRecord CreateExecution(Guid projectId, PlanExecutionRequest request, ApiRatePlan rate, ProjectBudget budget, string status, decimal costUsd, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), ProjectId = projectId, ApiRatePlanId = rate.Id,
        IdempotencyKey = request.IdempotencyKey.Trim(), Provider = request.Provider.Trim().ToLowerInvariant(),
        Model = request.Model.Trim().ToLowerInvariant(), Flow = request.Flow.Trim(), Status = status,
        ApprovalRequired = request.ApprovalRequired, ApprovedBy = request.ApprovalRequired ? string.Empty : "Política local",
        ApprovedAt = request.ApprovalRequired || status == "blocked" ? null : now,
        InputUnits = request.InputUnits, OutputUnits = request.OutputUnits, EstimatedCostUsd = costUsd,
        EstimatedCostGtq = Math.Round(costUsd * budget.ExchangeRateGtqPerUsd, 6), AttemptNumber = 1,
        ErrorCode = status == "blocked" ? "BUDGET_BLOCKED" : string.Empty,
        ErrorMessage = status == "blocked" ? StatusNote(status) : string.Empty,
        IsDemoData = true, CreatedAt = now,
    };

    private static ExecutionAudit CreateAudit(Guid projectId, Guid executionId, string eventType, string from, string to, string note, DateTimeOffset at) => new()
    {
        Id = Guid.NewGuid(), ProjectId = projectId, ExecutionRecordId = executionId, EventType = eventType,
        FromStatus = from, ToStatus = to, Note = note.Trim(), Actor = "Administrador local", OccurredAt = at,
    };

    private static string StatusNote(string status) => status == "blocked"
        ? "Bloqueada por pausa o por límite diario/mensual. No se realizó ninguna llamada externa."
        : status == "awaiting_approval" ? "Pendiente de aprobación humana." : "Lista para simulación local.";

    private static Dictionary<string, string[]> ValidateRate(RatePlanRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Provider) || request.Provider.Trim().Length > 80) errors["provider"] = ["El proveedor es obligatorio."];
        if (string.IsNullOrWhiteSpace(request.Model) || request.Model.Trim().Length > 120) errors["model"] = ["El modelo es obligatorio."];
        if (request.InputUsdPerMillion < 0 || request.OutputUsdPerMillion < 0) errors["rates"] = ["Las tarifas no pueden ser negativas."];
        return errors;
    }

    private static Dictionary<string, string[]> ValidateBudget(BudgetRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.DailyLimitUsd <= 0 || request.MonthlyLimitUsd <= 0 || request.DailyLimitUsd > request.MonthlyLimitUsd) errors["limits"] = ["Los límites deben ser positivos y el diario no puede superar el mensual."];
        if (request.WarningPercent is < 1 or > 100) errors["warningPercent"] = ["El umbral debe estar entre 1 y 100."];
        if (request.ExchangeRateGtqPerUsd <= 0) errors["exchangeRateGtqPerUsd"] = ["El tipo de cambio debe ser positivo."];
        return errors;
    }

    private static Dictionary<string, string[]> ValidatePlan(PlanExecutionRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Trim().Length > 160) errors["idempotencyKey"] = ["La clave de idempotencia es obligatoria y admite hasta 160 caracteres."];
        if (string.IsNullOrWhiteSpace(request.Provider)) errors["provider"] = ["El proveedor es obligatorio."];
        if (string.IsNullOrWhiteSpace(request.Model)) errors["model"] = ["El modelo es obligatorio."];
        if (string.IsNullOrWhiteSpace(request.Flow) || request.Flow.Trim().Length > 120) errors["flow"] = ["El flujo es obligatorio."];
        if (request.InputUnits < 0 || request.OutputUnits < 0 || request.InputUnits + (long)request.OutputUnits <= 0) errors["units"] = ["La ejecución necesita unidades de entrada o salida positivas."];
        return errors;
    }
}
