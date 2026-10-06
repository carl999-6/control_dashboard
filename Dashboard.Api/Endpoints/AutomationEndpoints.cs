using System.Collections.Concurrent;
using System.Globalization;
using Dashboard.Api.Contracts;
using Dashboard.Api.Data;
using Dashboard.Api.Domain;
using Dashboard.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Endpoints;

public static class AutomationEndpoints
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> ScheduleLocks = new();
    private static readonly string[] Frequencies = ["manual", "interval", "daily", "weekly"];

    public static IEndpointRouteBuilder MapAutomationEndpoints(this IEndpointRouteBuilder app)
    {
        var automations = app.MapGroup("/api/projects/{projectId:guid}/automations").RequireAuthorization();

        automations.MapGet("/", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects.SingleOrDefaultAsync(item => item.Id == projectId, ct);
            if (project is null) return Results.NotFound();
            await EnsureDefaults(project, db, ct);
            var items = await db.AutomationSchedules.AsNoTracking().Where(item => item.ProjectId == projectId)
                .OrderBy(item => item.DisplayName).ToListAsync(ct);
            return Results.Ok(items.Select(AutomationScheduleResponse.FromEntity));
        });

        automations.MapPut("/{workflow}", async (Guid projectId, string workflow, AutomationScheduleRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            if (!AutomationCatalog.Workflows.ContainsKey(workflow)) return Results.NotFound();
            var project = await db.Projects.SingleOrDefaultAsync(item => item.Id == projectId, ct);
            if (project is null) return Results.NotFound();
            await EnsureDefaults(project, db, ct);
            var item = await db.AutomationSchedules.SingleAsync(value => value.ProjectId == projectId && value.Workflow == workflow, ct);
            item.IsEnabled = request.IsEnabled;
            item.Frequency = request.Frequency.Trim().ToLowerInvariant();
            item.IntervalMinutes = item.Frequency == "interval" ? request.IntervalMinutes : null;
            item.LocalTime = request.LocalTime.Trim();
            item.DayOfWeek = item.Frequency == "weekly" ? request.DayOfWeek : null;
            item.MaxRunsPerDay = request.MaxRunsPerDay;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            item.NextRunAt = AutomationScheduleCalculator.CalculateNext(item, project.TimeZone, item.UpdatedAt);
            await db.SaveChangesAsync(ct);
            return Results.Ok(AutomationScheduleResponse.FromEntity(item));
        });

        automations.MapGet("/runs", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(item => item.Id == projectId, ct)) return Results.NotFound();
            var runs = (await db.AutomationRuns.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(ct))
                .OrderByDescending(item => item.CreatedAt).Take(100).ToList();
            return Results.Ok(runs.Select(AutomationRunResponse.FromEntity));
        });

        automations.MapPost("/{workflow}/run", async (Guid projectId, string workflow, DashboardDbContext db, AutomationExecutor executor, CancellationToken ct) =>
        {
            if (!AutomationCatalog.Workflows.ContainsKey(workflow)) return Results.NotFound();
            var project = await db.Projects.SingleOrDefaultAsync(item => item.Id == projectId, ct);
            if (project is null) return Results.NotFound();
            await EnsureDefaults(project, db, ct);
            var schedule = await db.AutomationSchedules.SingleAsync(item => item.ProjectId == projectId && item.Workflow == workflow, ct);
            var gate = ScheduleLocks.GetOrAdd(schedule.Id, _ => new SemaphoreSlim(1, 1));
            if (!await gate.WaitAsync(0, ct))
            {
                return Results.Conflict(new { title = "Esta automatización ya se está ejecutando." });
            }
            try
            {
                return Results.Ok(AutomationRunResponse.FromEntity(await executor.RunAsync(schedule, "manual", ct)));
            }
            finally
            {
                gate.Release();
            }
        });

        return app;
    }

    private static async Task EnsureDefaults(Project project, DashboardDbContext db, CancellationToken ct)
    {
        var existing = await db.AutomationSchedules.Where(item => item.ProjectId == project.Id)
            .Select(item => item.Workflow).ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;
        foreach (var definition in AutomationCatalog.Workflows)
        {
            if (existing.Contains(definition.Key, StringComparer.OrdinalIgnoreCase)) continue;
            var weekly = definition.Key == "telegram_weekly_summary";
            var summary = definition.Key.StartsWith("telegram_", StringComparison.Ordinal);
            var item = new AutomationSchedule
            {
                Id = Guid.NewGuid(), ProjectId = project.Id, Workflow = definition.Key, DisplayName = definition.Value,
                IsEnabled = summary, Frequency = weekly ? "weekly" : "daily", IntervalMinutes = null,
                LocalTime = weekly ? "08:00" : definition.Key == "telegram_daily_summary" ? "18:00" : "07:00",
                DayOfWeek = weekly ? 1 : null, MaxRunsPerDay = definition.Key is "seo_content_pipeline" or "x_response_pipeline" ? 2 : 1,
                UpdatedAt = now,
            };
            item.NextRunAt = AutomationScheduleCalculator.CalculateNext(item, project.TimeZone, now);
            db.AutomationSchedules.Add(item);
        }
        await db.SaveChangesAsync(ct);
    }

    private static Dictionary<string, string[]> Validate(AutomationScheduleRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var frequency = request.Frequency.Trim().ToLowerInvariant();
        if (!Frequencies.Contains(frequency)) errors["frequency"] = ["La frecuencia debe ser manual, interval, daily o weekly."];
        if (frequency == "interval" && request.IntervalMinutes is < 5 or > 10080) errors["intervalMinutes"] = ["El intervalo debe estar entre 5 y 10,080 minutos."];
        if (!TimeOnly.TryParseExact(request.LocalTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) errors["localTime"] = ["La hora local debe usar el formato HH:mm."];
        if (frequency == "weekly" && request.DayOfWeek is < 0 or > 6) errors["dayOfWeek"] = ["El día semanal debe estar entre 0 (domingo) y 6 (sábado)."];
        if (request.MaxRunsPerDay is < 1 or > 100) errors["maxRunsPerDay"] = ["El límite diario debe estar entre 1 y 100."];
        return errors;
    }
}
