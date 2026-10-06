using System.Text.RegularExpressions;
using Dashboard.Api.Contracts;
using Dashboard.Api.Data;
using Dashboard.Api.Domain;
using Dashboard.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Endpoints;

public static class XAssistantEndpoints
{
    private static readonly Dictionary<string, HashSet<string>> ProposalTransitions = new()
    {
        ["pending_review"] = ["approved", "copied", "published", "discarded"],
        ["approved"] = ["copied", "published", "discarded"],
        ["copied"] = ["published", "discarded"],
        ["published"] = [],
        ["discarded"] = [],
    };

    public static IEndpointRouteBuilder MapXAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/x-assistant").RequireAuthorization();

        group.MapGet("/settings", async (Guid projectId, XAssistantService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.GetSettingsAsync(projectId, ct)); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });

        group.MapPut("/settings", async (Guid projectId, XAssistantSettingsRequest request, XAssistantService service, CancellationToken ct) =>
        {
            var errors = ValidateSettings(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            try { return Results.Ok(await service.UpdateSettingsAsync(projectId, request, ct)); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        });

        group.MapGet("/sources", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(item => item.Id == projectId, ct)) return Results.NotFound();
            var items = (await db.XSourcePosts.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(ct))
                .OrderByDescending(item => item.PostedAt).Take(100);
            return Results.Ok(items.Select(XSourcePostResponse.FromEntity));
        });

        group.MapPost("/sources", async (Guid projectId, XSourcePostRequest request, XAssistantService service, CancellationToken ct) =>
        {
            var errors = ValidateSource(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            try
            {
                var item = await service.ImportManualAsync(projectId, request, ct);
                return Results.Ok(XSourcePostResponse.FromEntity(item));
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (ArgumentException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["url"] = [exception.Message] }); }
        });

        group.MapPost("/sources/{sourceId:guid}/dismiss", async (Guid projectId, Guid sourceId, DashboardDbContext db, CancellationToken ct) =>
        {
            var item = await db.XSourcePosts.SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Id == sourceId, ct);
            if (item is null) return Results.NotFound();
            if (item.Status == "proposed") return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["La publicación ya tiene una propuesta asociada."] });
            item.Status = "dismissed";
            await db.SaveChangesAsync(ct);
            return Results.Ok(XSourcePostResponse.FromEntity(item));
        });

        group.MapGet("/proposals", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(item => item.Id == projectId, ct)) return Results.NotFound();
            var items = (await db.XReplyProposals.AsNoTracking().Include(item => item.SourcePost)
                .Where(item => item.ProjectId == projectId).ToListAsync(ct)).OrderByDescending(item => item.CreatedAt).Take(100);
            return Results.Ok(items.Select(XReplyProposalResponse.FromEntity));
        });

        group.MapPost("/proposals/{proposalId:guid}/transition", async (Guid projectId, Guid proposalId,
            XProposalTransitionRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var proposal = await db.XReplyProposals.Include(item => item.SourcePost)
                .SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == proposalId, ct);
            if (proposal is null) return Results.NotFound();
            var target = request.TargetStatus?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!ProposalTransitions.TryGetValue(proposal.Status, out var allowed) || !allowed.Contains(target))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["targetStatus"] = [$"No se puede pasar de {proposal.Status} a {target}."] });
            var selected = string.IsNullOrWhiteSpace(request.SelectedReply) ? proposal.SelectedReply : request.SelectedReply.Trim();
            if (selected.EnumerateRunes().Count() is < 1 or > 235)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["selectedReply"] = ["La respuesta elegida debe contener entre 1 y 235 caracteres para reservar espacio al enlace medible."] });
            var publishedUrl = request.PublishedReplyUrl?.Trim() ?? string.Empty;
            if (target == "published")
            {
                try { _ = XAssistantService.ExtractPostId(publishedUrl); }
                catch (ArgumentException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["publishedReplyUrl"] = [exception.Message] }); }
            }
            var now = DateTimeOffset.UtcNow;
            proposal.SelectedReply = selected;
            proposal.Status = target;
            proposal.UpdatedAt = now;
            if (target == "published")
            {
                proposal.PublishedReplyUrl = publishedUrl;
                proposal.PublishedAt = now;
                db.SocialPosts.Add(new SocialPost
                {
                    Id = Guid.NewGuid(), ProjectId = projectId, Platform = "x", Topic = selected,
                    Format = "reply", Status = "published", ExternalUrl = publishedUrl, DataSource = "manual",
                    PublishedAt = now, Impressions = 0, Engagements = 0, Clicks = 0,
                    CreatedAt = now, UpdatedAt = now,
                });
            }
            await db.SaveChangesAsync(ct);
            return Results.Ok(XReplyProposalResponse.FromEntity(proposal));
        });

        return app;
    }

    private static Dictionary<string, string[]> ValidateSettings(XAssistantSettingsRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.ApiReadEnabled && string.IsNullOrWhiteSpace(request.SearchQuery)) errors["searchQuery"] = ["La búsqueda es obligatoria al habilitar la API."];
        if ((request.SearchQuery?.Length ?? 0) > 400) errors["searchQuery"] = ["La búsqueda admite hasta 400 caracteres para reservar espacio a filtros de seguridad."];
        if (!Regex.IsMatch(request.Language?.Trim() ?? string.Empty, "^[a-z]{2,3}$", RegexOptions.IgnoreCase)) errors["language"] = ["Usa un código de idioma de dos o tres letras."];
        if (request.MaximumPostsPerSync is < 10 or > 100) errors["maximumPostsPerSync"] = ["X admite entre 10 y 100 resultados por consulta reciente."];
        if (request.ReadCostUsdPerPost is < 0 or > 1) errors["readCostUsdPerPost"] = ["El costo estimado por post debe estar entre USD 0 y USD 1."];
        if (string.IsNullOrWhiteSpace(request.ToneInstructions) || request.ToneInstructions.Trim().Length > 2000) errors["toneInstructions"] = ["Define el tono en hasta 2,000 caracteres."];
        if ((request.LandingPath?.Length ?? 0) > 500) errors["landingPath"] = ["La ruta de destino admite hasta 500 caracteres."];
        if (!string.IsNullOrWhiteSpace(request.LandingPath) && (Uri.TryCreate(request.LandingPath, UriKind.Absolute, out _) || !request.LandingPath.Trim().StartsWith('/'))) errors["landingPath"] = ["Usa una ruta interna que empiece con /, por ejemplo /servicios."];
        if (string.IsNullOrWhiteSpace(request.UtmCampaign) || request.UtmCampaign.Trim().Length > 120) errors["utmCampaign"] = ["La campaña UTM es obligatoria."];
        else if (!Regex.IsMatch(request.UtmCampaign, "[A-Za-z0-9]")) errors["utmCampaign"] = ["La campaña UTM debe incluir al menos una letra o un número."];
        return errors;
    }

    private static Dictionary<string, string[]> ValidateSource(XSourcePostRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Url) || request.Url.Length > 1000) errors["url"] = ["La URL del post es obligatoria."];
        if (string.IsNullOrWhiteSpace(request.AuthorUsername) || request.AuthorUsername.Trim().TrimStart('@').Length > 100) errors["authorUsername"] = ["El usuario es obligatorio."];
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Trim().Length > 10000) errors["text"] = ["Copia el texto del post en hasta 10,000 caracteres."];
        if (!Regex.IsMatch(request.Language?.Trim() ?? string.Empty, "^[a-z]{2,3}$", RegexOptions.IgnoreCase)) errors["language"] = ["Usa un código de idioma de dos o tres letras."];
        if (new[] { request.LikeCount, request.ReplyCount, request.RepostCount, request.QuoteCount, request.ImpressionCount }.Any(value => value < 0)) errors["metrics"] = ["Las métricas no pueden ser negativas."];
        return errors;
    }
}
