using Dashboard.Api.Contracts;
using Dashboard.Api.Data;
using Dashboard.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Endpoints;

public static class PostHogEndpoints
{
    public static IEndpointRouteBuilder MapPostHogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/integrations/posthog").RequireAuthorization();
        group.MapGet("/status", async (Guid projectId, DashboardDbContext db, PostHogService service, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(value => value.Id == projectId, ct)) return Results.NotFound();
            return Results.Ok(await service.GetStatusAsync(projectId, ct));
        });
        group.MapPut("/settings", async (Guid projectId, PostHogSettingsRequest request, DashboardDbContext db, PostHogService service, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(value => value.Id == projectId, ct)) return Results.NotFound();
            var errors = PostHogService.Validate(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var current = await db.PostHogConnections.AsNoTracking().SingleOrDefaultAsync(value => value.ProjectId == projectId, ct);
            if (current is not null && (current.Region != request.Region || current.ExternalProjectId != request.ExternalProjectId) &&
                !request.ConfirmReplaceData && await db.PostHogMetrics.AnyAsync(value => value.ProjectId == projectId, ct))
                return Results.Conflict(new { title = "Cambiar región o proyecto descartará los agregados locales anteriores. Confirma el reemplazo para continuar." });
            return Results.Ok(await service.SaveSettingsAsync(projectId, request, ct));
        });
        group.MapGet("/summary", async (Guid projectId, int? days, DashboardDbContext db, PostHogService service, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(value => value.Id == projectId, ct)) return Results.NotFound();
            if (days is < 1 or > 90) return Results.ValidationProblem(new Dictionary<string, string[]> { ["days"] = ["El rango debe estar entre 1 y 90 días."] });
            return Results.Ok(await service.GetSummaryAsync(projectId, days ?? 28, ct));
        });
        group.MapGet("/events", async (Guid projectId, int? days, string? source, string? campaign, string? content,
            DashboardDbContext db, PostHogService service, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(value => value.Id == projectId, ct)) return Results.NotFound();
            try { return Results.Ok(await service.GetRecentEventsAsync(projectId, days ?? 28, source, campaign, content, ct)); }
            catch (PostHogApiException exception) when (exception.Code == "POSTHOG_INVALID_CONFIGURATION")
            { return Results.ValidationProblem(new Dictionary<string, string[]> { ["filters"] = [exception.Message] }); }
            catch (PostHogApiException exception) when (exception.Code is "POSTHOG_NOT_CONNECTED" or "POSTHOG_KEY_NOT_CONFIGURED")
            { return Results.Conflict(new { code = exception.Code, message = exception.Message }); }
            catch (PostHogApiException exception)
            { return Results.Problem(title: exception.Message, statusCode: StatusCodes.Status502BadGateway); }
        });
        return app;
    }
}
