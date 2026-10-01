using System.Collections.Concurrent;
using Dashboard.Api.Contracts;
using Dashboard.Api.Data;
using Dashboard.Api.Domain;
using Dashboard.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Endpoints;

public static class NotificationEndpoints
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> ProjectLocks = new();
    private static readonly Dictionary<string, int> SeverityRank = new(StringComparer.OrdinalIgnoreCase)
    {
        ["info"] = 1, ["warning"] = 2, ["critical"] = 3,
    };

    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var notifications = app.MapGroup("/api/projects/{projectId:guid}/notifications").RequireAuthorization();

        notifications.MapGet("/policy", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            return Results.Ok(NotificationPolicyResponse.FromEntity(await GetOrCreatePolicy(projectId, db, ct)));
        });

        notifications.MapPut("/policy", async (Guid projectId, NotificationPolicyRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var errors = ValidatePolicy(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var policy = await GetOrCreatePolicy(projectId, db, ct);
            policy.IsEnabled = request.IsEnabled;
            policy.MinimumSeverity = request.MinimumSeverity.Trim().ToLowerInvariant();
            policy.GroupWindowMinutes = request.GroupWindowMinutes;
            policy.QuietHoursStart = request.QuietHoursStart;
            policy.QuietHoursEnd = request.QuietHoursEnd;
            policy.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(NotificationPolicyResponse.FromEntity(policy));
        });

        notifications.MapGet("/", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var items = (await db.Notifications.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(ct))
                .OrderByDescending(item => item.UpdatedAt).Take(100).Select(NotificationResponse.FromEntity);
            return Results.Ok(items);
        });

        notifications.MapGet("/summary", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var items = await db.Notifications.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(ct);
            return Results.Ok(new NotificationSummaryResponse(
                items.Count(item => item.Status == "sent"), items.Count(item => item.Status == "queued"),
                items.Count(item => item.Status == "failed"), items.Count(item => item.Status == "grouped"),
                items.Count(item => item.Status == "suppressed"), items.Sum(item => item.GroupCount)));
        });

        notifications.MapPost("/dispatch", async (Guid projectId, DispatchNotificationRequest request, DashboardDbContext db, INotificationChannel channel, CancellationToken ct) =>
        {
            var errors = ValidateDispatch(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();

            var gate = ProjectLocks.GetOrAdd(projectId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(ct);
            try
            {
                var policy = await GetOrCreatePolicy(projectId, db, ct);
                var now = DateTimeOffset.UtcNow;
                var key = request.DeduplicationKey.Trim();
                var grouped = (await db.Notifications.Where(item => item.ProjectId == projectId && item.DeduplicationKey == key)
                    .ToListAsync(ct)).Where(item => item.UpdatedAt >= now.AddMinutes(-policy.GroupWindowMinutes))
                    .OrderByDescending(item => item.UpdatedAt).FirstOrDefault();
                if (grouped is not null)
                {
                    grouped.GroupCount++;
                    grouped.UpdatedAt = now;
                    if (grouped.Status is not ("failed" or "queued" or "suppressed")) grouped.Status = "grouped";
                    await db.SaveChangesAsync(ct);
                    return Results.Ok(NotificationResponse.FromEntity(grouped));
                }

                var item = new NotificationRecord
                {
                    Id = Guid.NewGuid(), ProjectId = projectId, NotificationPolicyId = policy.Id,
                    DeduplicationKey = key, Category = request.Category.Trim().ToLowerInvariant(), Severity = request.Severity.Trim().ToLowerInvariant(),
                    Title = request.Title.Trim(), Message = request.Message.Trim(), Status = "queued", GroupCount = 1,
                    AttemptCount = 0, SimulateFailure = request.SimulateFailure, ErrorMessage = string.Empty,
                    IsDemoData = true, CreatedAt = now, UpdatedAt = now,
                };
                if (!policy.IsEnabled)
                {
                    item.Status = "suppressed";
                    item.ErrorMessage = "Las notificaciones están desactivadas para este proyecto.";
                }
                else if (SeverityRank[item.Severity] < SeverityRank[policy.MinimumSeverity])
                {
                    item.Status = "suppressed";
                    item.ErrorMessage = "La severidad no alcanza el umbral configurado.";
                }
                else if (await IsQuietHour(projectId, policy, db, ct))
                {
                    item.Status = "queued";
                    item.ErrorMessage = "En cola hasta que termine el horario silencioso.";
                }
                else
                {
                    await Deliver(item, channel, ct);
                }
                db.Notifications.Add(item);
                await db.SaveChangesAsync(ct);
                return Results.Created($"/api/projects/{projectId}/notifications/{item.Id}", NotificationResponse.FromEntity(item));
            }
            finally { gate.Release(); }
        });

        notifications.MapPost("/{notificationId:guid}/retry", async (Guid projectId, Guid notificationId, DashboardDbContext db, INotificationChannel channel, CancellationToken ct) =>
        {
            var item = await db.Notifications.SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Id == notificationId, ct);
            if (item is null) return Results.NotFound();
            if (item.Status is not ("failed" or "queued")) return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Solo se pueden recuperar notificaciones fallidas o en cola."] });
            item.SimulateFailure = false;
            await Deliver(item, channel, ct);
            await db.SaveChangesAsync(ct);
            return Results.Ok(NotificationResponse.FromEntity(item));
        });

        notifications.MapPost("/process-queued", async (Guid projectId, DashboardDbContext db, INotificationChannel channel, CancellationToken ct) =>
        {
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var policy = await GetOrCreatePolicy(projectId, db, ct);
            if (await IsQuietHour(projectId, policy, db, ct)) return Results.Ok(Array.Empty<NotificationResponse>());
            var queued = await db.Notifications.Where(item => item.ProjectId == projectId && item.Status == "queued").ToListAsync(ct);
            foreach (var item in queued) await Deliver(item, channel, ct);
            await db.SaveChangesAsync(ct);
            return Results.Ok(queued.Select(NotificationResponse.FromEntity));
        });

        return app;
    }

    private static async Task Deliver(NotificationRecord item, INotificationChannel channel, CancellationToken ct)
    {
        item.AttemptCount++;
        item.LastAttemptAt = DateTimeOffset.UtcNow;
        var result = await channel.DeliverAsync(item, ct);
        item.UpdatedAt = item.LastAttemptAt.Value;
        item.Status = result.Delivered ? "sent" : "failed";
        item.DeliveredAt = result.Delivered ? item.LastAttemptAt : null;
        item.ErrorMessage = result.ErrorMessage;
    }

    private static async Task<NotificationPolicy> GetOrCreatePolicy(Guid projectId, DashboardDbContext db, CancellationToken ct)
    {
        var policy = await db.NotificationPolicies.SingleOrDefaultAsync(item => item.ProjectId == projectId, ct);
        if (policy is not null) return policy;
        policy = new NotificationPolicy
        {
            Id = Guid.NewGuid(), ProjectId = projectId, IsEnabled = true, DeliveryMode = "simulated",
            MinimumSeverity = "info", GroupWindowMinutes = 30, QuietHoursStart = 22, QuietHoursEnd = 7,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.NotificationPolicies.Add(policy);
        await db.SaveChangesAsync(ct);
        return policy;
    }

    private static async Task<bool> IsQuietHour(Guid projectId, NotificationPolicy policy, DashboardDbContext db, CancellationToken ct)
    {
        if (policy.QuietHoursStart == policy.QuietHoursEnd) return false;
        var zoneId = await db.Projects.AsNoTracking().Where(item => item.Id == projectId).Select(item => item.TimeZone).SingleAsync(ct);
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId); }
        catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { zone = TimeZoneInfo.Utc; }
        var hour = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).Hour;
        return policy.QuietHoursStart < policy.QuietHoursEnd
            ? hour >= policy.QuietHoursStart && hour < policy.QuietHoursEnd
            : hour >= policy.QuietHoursStart || hour < policy.QuietHoursEnd;
    }

    private static async Task<bool> ProjectExists(Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        await db.Projects.AnyAsync(item => item.Id == projectId, ct);

    private static Dictionary<string, string[]> ValidatePolicy(NotificationPolicyRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (!SeverityRank.ContainsKey(request.MinimumSeverity.Trim())) errors["minimumSeverity"] = ["La severidad mínima debe ser info, warning o critical."];
        if (request.GroupWindowMinutes is < 1 or > 1440) errors["groupWindowMinutes"] = ["La ventana de agrupación debe estar entre 1 y 1440 minutos."];
        if (request.QuietHoursStart is < 0 or > 23 || request.QuietHoursEnd is < 0 or > 23) errors["quietHours"] = ["Las horas deben estar entre 0 y 23."];
        return errors;
    }

    private static Dictionary<string, string[]> ValidateDispatch(DispatchNotificationRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.DeduplicationKey) || request.DeduplicationKey.Trim().Length > 160) errors["deduplicationKey"] = ["La clave de deduplicación es obligatoria y admite hasta 160 caracteres."];
        if (string.IsNullOrWhiteSpace(request.Category) || request.Category.Trim().Length > 40) errors["category"] = ["La categoría es obligatoria."];
        if (!SeverityRank.ContainsKey(request.Severity.Trim())) errors["severity"] = ["La severidad debe ser info, warning o critical."];
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 180) errors["title"] = ["El título es obligatorio y admite hasta 180 caracteres."];
        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Trim().Length > 1200) errors["message"] = ["El mensaje es obligatorio y admite hasta 1200 caracteres."];
        return errors;
    }
}
