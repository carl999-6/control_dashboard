using Dashboard.Api.Contracts;
using Dashboard.Api.Data;
using Dashboard.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Endpoints;

public static class WordPressEndpoints
{
    public static IEndpointRouteBuilder MapWordPressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/integrations/wordpress").RequireAuthorization();
        group.MapGet("/settings", async (Guid projectId, WordPressService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.GetSettingsAsync(projectId, ct)); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });
        group.MapPut("/settings", async (Guid projectId, WordPressSettingsRequest request, WordPressService service, CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            try { return Results.Ok(await service.UpdateSettingsAsync(projectId, request, ct)); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });
        group.MapPost("/test", async (Guid projectId, WordPressService service, CancellationToken ct) =>
        {
            try
            {
                var result = await service.TestConnectionAsync(projectId, ct);
                return result.Succeeded ? Results.Ok(result) : Results.Problem(result.Message, statusCode: 409, title: result.Status);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });
        group.MapPost("/content/{contentId:guid}/draft", async (Guid projectId, Guid contentId, WordPressService service,
            DashboardDbContext db, CancellationToken ct) =>
        {
            try
            {
                var result = await service.CreateDraftAsync(projectId, contentId, "manual", string.Empty, null, null, ct);
                if (!result.Succeeded) return Results.Problem(result.ErrorMessage, statusCode: 409, title: result.ErrorCode);
                var item = await db.ContentPieces.AsNoTracking().Include(value => value.History)
                    .SingleAsync(value => value.ProjectId == projectId && value.Id == contentId, ct);
                return Results.Ok(ContentPieceResponse.FromEntity(item));
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });
        return app;
    }

    private static Dictionary<string, string[]> Validate(WordPressSettingsRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (!Uri.TryCreate(request.BaseUrl?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            errors["baseUrl"] = ["La URL debe ser HTTPS y no incluir consulta ni fragmento."];
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Trim().Length > 160)
            errors["username"] = ["El usuario de WordPress es obligatorio y admite hasta 160 caracteres."];
        return errors;
    }
}
