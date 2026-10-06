using System.Text.RegularExpressions;
using Dashboard.Api.Contracts;
using Dashboard.Api.Services;

namespace Dashboard.Api.Endpoints;

public static class GeminiEndpoints
{
    public static IEndpointRouteBuilder MapGeminiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/integrations/gemini").RequireAuthorization();

        group.MapGet("/settings", async (Guid projectId, GeminiSeoService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.GetSettingsAsync(projectId, ct)); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });

        group.MapPut("/settings", async (Guid projectId, GeminiSettingsRequest request, GeminiSeoService service, CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            try { return Results.Ok(await service.UpdateSettingsAsync(projectId, request, ct)); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });

        return app;
    }

    private static Dictionary<string, string[]> Validate(GeminiSettingsRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Model) || request.Model.Trim().Length > 120 || !Regex.IsMatch(request.Model.Trim(), "^[a-zA-Z0-9._-]+$"))
            errors["model"] = ["Indica un identificador de modelo válido de Gemini."];
        if (request.MinimumImpressions is < 1 or > 1_000_000) errors["minimumImpressions"] = ["Las impresiones mínimas deben estar entre 1 y 1,000,000."];
        if (request.MinimumPosition is < 1 or > 100 || request.MaximumPosition is < 1 or > 100 || request.MinimumPosition > request.MaximumPosition)
            errors["position"] = ["El rango de posiciones debe estar entre 1 y 100 y conservar el orden."];
        if (request.MaximumCtrPercent is < 0 or > 100) errors["maximumCtrPercent"] = ["El CTR debe estar entre 0% y 100%."];
        if (request.MaximumSeoDraftsPerDay is < 1 or > 10) errors["maximumSeoDraftsPerDay"] = ["El límite diario debe estar entre 1 y 10 borradores."];
        if (request.MaximumXProposalsPerDay is < 1 or > 20) errors["maximumXProposalsPerDay"] = ["El límite de propuestas para X debe estar entre 1 y 20."];
        if (request.MaximumTotalGeminiRunsPerDay is < 1 or > 30 || request.MaximumTotalGeminiRunsPerDay < request.MaximumSeoDraftsPerDay || request.MaximumTotalGeminiRunsPerDay < request.MaximumXProposalsPerDay)
            errors["maximumTotalGeminiRunsPerDay"] = ["El límite compartido debe cubrir al menos el mayor límite individual y estar entre 1 y 30."];
        if (request.MinimumDraftWords is < 300 or > 2500) errors["minimumDraftWords"] = ["La extensión mínima debe estar entre 300 y 2,500 palabras."];
        if (request.MaximumOutputTokens is < 512 or > 8192) errors["maximumOutputTokens"] = ["El máximo de salida debe estar entre 512 y 8,192 tokens."];
        if (string.IsNullOrWhiteSpace(request.Owner) || request.Owner.Trim().Length > 120) errors["owner"] = ["El responsable es obligatorio."];
        return errors;
    }
}
