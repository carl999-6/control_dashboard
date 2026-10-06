using Dashboard.Api.Contracts;
using Dashboard.Api.Data;
using Dashboard.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Endpoints;

public static class SearchConsoleEndpoints
{
    public static IEndpointRouteBuilder MapSearchConsoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/integrations/search-console").RequireAuthorization();

        group.MapGet("/status", async (Guid projectId, DashboardDbContext db, SearchConsoleService service, CancellationToken ct) =>
            await db.Projects.AnyAsync(item => item.Id == projectId, ct)
                ? Results.Ok(await service.GetStatusAsync(projectId, ct))
                : Results.NotFound());

        group.MapPost("/authorize", async (Guid projectId, HttpRequest request, SearchConsoleService service, IConfiguration configuration, CancellationToken ct) =>
        {
            var redirectUri = GetRedirectUri(request, configuration);
            try
            {
                return Results.Ok(new SearchConsoleAuthorizationResponse(await service.CreateAuthorizationUrlAsync(projectId, redirectUri, ct)));
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["configuration"] = [exception.Message] });
            }
        });

        group.MapPut("/settings", async (Guid projectId, SearchConsoleSettingsRequest settings, SearchConsoleService service, CancellationToken ct) =>
        {
            var errors = ValidateSettings(settings);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var result = await service.UpdateSettingsAsync(projectId, settings, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapGet("/summary", async (Guid projectId, DashboardDbContext db, SearchConsoleService service, CancellationToken ct) =>
            await db.Projects.AnyAsync(item => item.Id == projectId, ct)
                ? Results.Ok(await service.GetSummaryAsync(projectId, ct))
                : Results.NotFound());

        group.MapDelete("/connection", async (Guid projectId, [FromBody] SearchConsoleDisconnectRequest request, SearchConsoleService service, CancellationToken ct) =>
        {
            if (!string.Equals(request.Confirmation?.Trim(), "desconectar", StringComparison.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["confirmation"] = ["Escribe exactamente “desconectar” para confirmar."] });
            return await service.DisconnectAsync(projectId, ct) ? Results.NoContent() : Results.NotFound();
        });

        app.MapGet("/api/integrations/search-console/callback", async (
            string? code,
            string? state,
            string? error,
            HttpRequest request,
            SearchConsoleService service,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var webBase = service.WebBaseUrl;
            if (!string.IsNullOrWhiteSpace(error)) return Results.Redirect($"{webBase}/automatizaciones?searchConsole=denied");
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state)) return Results.Redirect($"{webBase}/automatizaciones?searchConsole=invalid");
            try
            {
                await service.CompleteAuthorizationAsync(code, state, GetRedirectUri(request, configuration), ct);
                return Results.Redirect($"{webBase}/automatizaciones?searchConsole=connected");
            }
            catch (Exception exception) when (exception is InvalidOperationException or SearchConsoleApiException or HttpRequestException or TaskCanceledException)
            {
                loggerFactory.CreateLogger("SearchConsoleOAuth").LogWarning("No se pudo completar OAuth de Search Console: {Reason}", exception.Message);
                return Results.Redirect($"{webBase}/automatizaciones?searchConsole=error");
            }
        });

        return app;
    }

    private static string GetRedirectUri(HttpRequest request, IConfiguration configuration)
    {
        var configured = configuration["GOOGLE_SEARCH_CONSOLE_REDIRECT_URI"]?.Trim();
        return string.IsNullOrWhiteSpace(configured)
            ? $"{request.Scheme}://{request.Host}/api/integrations/search-console/callback"
            : configured;
    }

    private static Dictionary<string, string[]> ValidateSettings(SearchConsoleSettingsRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.LookbackDays is < 7 or > 90) errors["lookbackDays"] = ["El rango debe estar entre 7 y 90 días."];
        if (request.RowLimit is < 1 or > 25000) errors["rowLimit"] = ["El límite debe estar entre 1 y 25,000 filas."];
        return errors;
    }
}
