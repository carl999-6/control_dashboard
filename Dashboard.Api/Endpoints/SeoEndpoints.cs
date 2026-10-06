using System.Text;
using System.Text.RegularExpressions;
using Dashboard.Api.Contracts;
using Dashboard.Api.Data;
using Dashboard.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Endpoints;

public static class SeoEndpoints
{
    private static readonly HashSet<string> OpportunityStatuses = ["detected", "selected", "converted", "dismissed"];
    private static readonly HashSet<string> ContentTypes = ["new", "update", "merge"];
    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new()
    {
        ["brief"] = ["draft", "discarded"],
        ["draft"] = ["pending_review", "discarded"],
        ["pending_review"] = ["draft", "approved", "discarded"],
        ["approved"] = ["scheduled", "discarded"],
        ["scheduled"] = ["approved", "discarded"],
        ["sent_draft"] = ["published", "measured"],
        ["published"] = ["measured"],
        ["measured"] = [],
        ["discarded"] = ["brief"],
    };

    public static IEndpointRouteBuilder MapSeoEndpoints(this IEndpointRouteBuilder app)
    {
        var seo = app.MapGroup("/api/projects/{projectId:guid}/seo").RequireAuthorization();

        seo.MapGet("/opportunities", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var items = (await db.SeoOpportunities.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(ct))
                .OrderByDescending(item => item.UpdatedAt).Select(SeoOpportunityResponse.FromEntity);
            return Results.Ok(items);
        });

        seo.MapPost("/opportunities", async (Guid projectId, SeoOpportunityRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var errors = ValidateOpportunity(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var now = DateTimeOffset.UtcNow;
            var item = new SeoOpportunity
            {
                Id = Guid.NewGuid(), ProjectId = projectId, Query = request.Query.Trim(),
                TargetPage = request.TargetPage?.Trim() ?? string.Empty, Evidence = request.Evidence.Trim(),
                Hypothesis = request.Hypothesis.Trim(), Status = request.Status.Trim().ToLowerInvariant(),
                DataSource = "manual", BaselineImpressions = request.BaselineImpressions,
                BaselineClicks = request.BaselineClicks, IsDemoData = false, CreatedAt = now, UpdatedAt = now,
            };
            db.SeoOpportunities.Add(item);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{projectId}/seo/opportunities/{item.Id}", SeoOpportunityResponse.FromEntity(item));
        });

        seo.MapPut("/opportunities/{opportunityId:guid}", async (Guid projectId, Guid opportunityId, SeoOpportunityRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var errors = ValidateOpportunity(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var item = await db.SeoOpportunities.SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Id == opportunityId, ct);
            if (item is null) return Results.NotFound();
            item.Query = request.Query.Trim(); item.TargetPage = request.TargetPage?.Trim() ?? string.Empty;
            item.Evidence = request.Evidence.Trim(); item.Hypothesis = request.Hypothesis.Trim();
            item.Status = request.Status.Trim().ToLowerInvariant(); item.BaselineImpressions = request.BaselineImpressions;
            item.BaselineClicks = request.BaselineClicks; item.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(SeoOpportunityResponse.FromEntity(item));
        });

        seo.MapDelete("/opportunities/{opportunityId:guid}", async (Guid projectId, Guid opportunityId, DashboardDbContext db, CancellationToken ct) =>
        {
            var item = await db.SeoOpportunities.SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Id == opportunityId, ct);
            if (item is null) return Results.NotFound();
            db.SeoOpportunities.Remove(item);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        seo.MapGet("/content", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var items = (await db.ContentPieces.AsNoTracking().Include(item => item.History)
                    .Where(item => item.ProjectId == projectId).ToListAsync(ct))
                .OrderByDescending(item => item.UpdatedAt).Select(ContentPieceResponse.FromEntity);
            return Results.Ok(items);
        });

        seo.MapPost("/content", async (Guid projectId, ContentPieceRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var errors = await ValidateContent(request, projectId, db, ct);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var now = DateTimeOffset.UtcNow;
            var item = new ContentPiece
            {
                Id = Guid.NewGuid(), ProjectId = projectId, SeoOpportunityId = request.SeoOpportunityId,
                Title = request.Title.Trim(), Slug = NormalizeSlug(request.Title), ContentType = request.ContentType.Trim().ToLowerInvariant(),
                PrimaryKeyword = request.PrimaryKeyword.Trim(), SearchIntent = request.SearchIntent.Trim().ToLowerInvariant(),
                Hypothesis = request.Hypothesis.Trim(), BaselineSummary = request.BaselineSummary.Trim(),
                Objective = request.Objective.Trim(), Owner = request.Owner.Trim(), Brief = request.Brief?.Trim() ?? string.Empty,
                DraftMarkdown = request.DraftMarkdown?.Trim() ?? string.Empty, MetaTitle = request.MetaTitle?.Trim() ?? string.Empty,
                MetaDescription = request.MetaDescription?.Trim() ?? string.Empty, Status = "brief",
                ScheduledFor = request.ScheduledFor, ResultNotes = string.Empty, SimulatedWordPressUrl = string.Empty,
                IsDemoData = false, CreatedAt = now, UpdatedAt = now,
            };
            item.History.Add(CreateHistory(projectId, item.Id, string.Empty, "brief", "Pieza creada manualmente.", now));
            db.ContentPieces.Add(item);
            if (request.SeoOpportunityId.HasValue)
            {
                var opportunity = await db.SeoOpportunities.SingleAsync(value => value.Id == request.SeoOpportunityId && value.ProjectId == projectId, ct);
                opportunity.Status = "selected"; opportunity.UpdatedAt = now;
            }
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{projectId}/seo/content/{item.Id}", ContentPieceResponse.FromEntity(item));
        });

        seo.MapPut("/content/{contentId:guid}", async (Guid projectId, Guid contentId, ContentPieceRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var errors = await ValidateContent(request, projectId, db, ct);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var item = await db.ContentPieces.Include(value => value.History)
                .SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Id == contentId, ct);
            if (item is null) return Results.NotFound();
            item.SeoOpportunityId = request.SeoOpportunityId; item.Title = request.Title.Trim(); item.Slug = NormalizeSlug(request.Title);
            item.ContentType = request.ContentType.Trim().ToLowerInvariant(); item.PrimaryKeyword = request.PrimaryKeyword.Trim();
            item.SearchIntent = request.SearchIntent.Trim().ToLowerInvariant(); item.Hypothesis = request.Hypothesis.Trim();
            item.BaselineSummary = request.BaselineSummary.Trim(); item.Objective = request.Objective.Trim(); item.Owner = request.Owner.Trim();
            item.Brief = request.Brief?.Trim() ?? string.Empty; item.DraftMarkdown = request.DraftMarkdown?.Trim() ?? string.Empty;
            item.MetaTitle = request.MetaTitle?.Trim() ?? string.Empty; item.MetaDescription = request.MetaDescription?.Trim() ?? string.Empty;
            item.ScheduledFor = request.ScheduledFor; item.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ContentPieceResponse.FromEntity(item));
        });

        seo.MapDelete("/content/{contentId:guid}", async (Guid projectId, Guid contentId, DashboardDbContext db, CancellationToken ct) =>
        {
            var item = await db.ContentPieces.SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Id == contentId, ct);
            if (item is null) return Results.NotFound();
            db.ContentPieces.Remove(item);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        seo.MapPost("/content/{contentId:guid}/transition", async (Guid projectId, Guid contentId, EditorialTransitionRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var item = await db.ContentPieces.Include(value => value.History)
                .SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Id == contentId, ct);
            if (item is null) return Results.NotFound();
            var target = request.TargetStatus?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!AllowedTransitions.TryGetValue(item.Status, out var allowed) || !allowed.Contains(target))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["targetStatus"] = [$"No se puede pasar de {item.Status} a {target}."] });
            if (target == "scheduled" && request.ScheduledFor is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["scheduledFor"] = ["La fecha editorial es obligatoria al programar."] });
            var now = DateTimeOffset.UtcNow;
            var previous = item.Status; item.Status = target; item.UpdatedAt = now;
            if (target == "scheduled") item.ScheduledFor = request.ScheduledFor;
            db.EditorialHistory.Add(CreateHistory(projectId, item.Id, previous, target, request.Note, now));
            await db.SaveChangesAsync(ct);
            return Results.Ok(ContentPieceResponse.FromEntity(item));
        });

        seo.MapPut("/content/{contentId:guid}/measurement", async (Guid projectId, Guid contentId, ContentMeasurementRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var item = await db.ContentPieces.Include(value => value.History)
                .SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Id == contentId, ct);
            if (item is null) return Results.NotFound();
            if (item.Status is not ("sent_draft" or "published"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["La medición requiere una pieza enviada como borrador o publicada."] });
            if (request.Impressions < 0 || request.Clicks < 0 || request.Clicks > request.Impressions)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["metrics"] = ["Las métricas deben ser positivas y los clics no pueden superar las impresiones."] });
            var now = DateTimeOffset.UtcNow; var previous = item.Status;
            item.ResultImpressions = request.Impressions; item.ResultClicks = request.Clicks;
            item.ResultNotes = request.Notes?.Trim() ?? string.Empty; item.MeasuredAt = request.MeasuredAt ?? now;
            item.Status = "measured"; item.UpdatedAt = now;
            db.EditorialHistory.Add(CreateHistory(projectId, item.Id, previous, "measured", "Resultado agregado registrado manualmente.", now));
            await db.SaveChangesAsync(ct);
            return Results.Ok(ContentPieceResponse.FromEntity(item));
        });

        seo.MapGet("/summary", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var opportunities = await db.SeoOpportunities.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(ct);
            var content = await db.ContentPieces.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(ct);
            return Results.Ok(new SeoSummaryResponse(
                opportunities.Count, content.Count(item => item.Status is not ("measured" or "discarded")),
                content.Count(item => item.Status == "pending_review"), content.Count(item => item.Status == "scheduled"),
                content.Count(item => item.Status == "measured"),
                opportunities.Any(item => item.IsDemoData) || content.Any(item => item.IsDemoData)));
        });

        return app;
    }

    private static async Task<bool> ProjectExists(Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        await db.Projects.AnyAsync(item => item.Id == projectId, ct);

    private static Dictionary<string, string[]> ValidateOpportunity(SeoOpportunityRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Query) || request.Query.Trim().Length > 220) errors["query"] = ["La consulta es obligatoria y admite hasta 220 caracteres."];
        if (string.IsNullOrWhiteSpace(request.Evidence) || request.Evidence.Trim().Length > 1000) errors["evidence"] = ["La evidencia es obligatoria y admite hasta 1,000 caracteres."];
        if (string.IsNullOrWhiteSpace(request.Hypothesis) || request.Hypothesis.Trim().Length > 1000) errors["hypothesis"] = ["La hipótesis es obligatoria y admite hasta 1,000 caracteres."];
        if (!OpportunityStatuses.Contains(request.Status?.Trim().ToLowerInvariant() ?? string.Empty)) errors["status"] = ["El estado de oportunidad no es válido."];
        if (request.BaselineImpressions < 0 || request.BaselineClicks < 0 || request.BaselineClicks > request.BaselineImpressions) errors["baseline"] = ["La línea base no es válida."];
        return errors;
    }

    private static async Task<Dictionary<string, string[]>> ValidateContent(ContentPieceRequest request, Guid projectId, DashboardDbContext db, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 220) errors["title"] = ["El título es obligatorio y admite hasta 220 caracteres."];
        if (!ContentTypes.Contains(request.ContentType?.Trim().ToLowerInvariant() ?? string.Empty)) errors["contentType"] = ["El tipo debe ser new, update o merge."];
        if (string.IsNullOrWhiteSpace(request.PrimaryKeyword) || request.PrimaryKeyword.Trim().Length > 220) errors["primaryKeyword"] = ["La palabra clave principal es obligatoria."];
        if (string.IsNullOrWhiteSpace(request.Hypothesis) || request.Hypothesis.Trim().Length > 1200) errors["hypothesis"] = ["La hipótesis es obligatoria."];
        if (string.IsNullOrWhiteSpace(request.BaselineSummary) || request.BaselineSummary.Trim().Length > 1200) errors["baselineSummary"] = ["La línea base es obligatoria."];
        if (string.IsNullOrWhiteSpace(request.Objective) || request.Objective.Trim().Length > 800) errors["objective"] = ["El objetivo es obligatorio."];
        if (string.IsNullOrWhiteSpace(request.Owner) || request.Owner.Trim().Length > 120) errors["owner"] = ["El responsable es obligatorio."];
        if ((request.Brief?.Length ?? 0) > 10000) errors["brief"] = ["El brief admite hasta 10,000 caracteres."];
        if ((request.DraftMarkdown?.Length ?? 0) > 60000) errors["draftMarkdown"] = ["El borrador admite hasta 60,000 caracteres."];
        if (request.SeoOpportunityId.HasValue && !await db.SeoOpportunities.AnyAsync(item => item.Id == request.SeoOpportunityId && item.ProjectId == projectId, ct)) errors["seoOpportunityId"] = ["La oportunidad no pertenece a este proyecto."];
        return errors;
    }

    private static EditorialHistory CreateHistory(Guid projectId, Guid contentId, string from, string to, string? note, DateTimeOffset at) => new()
    {
        Id = Guid.NewGuid(), ProjectId = projectId, ContentPieceId = contentId, FromStatus = from,
        ToStatus = to, Note = note?.Trim() ?? string.Empty, Actor = "Administrador local", ChangedAt = at,
    };

    private static string NormalizeSlug(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return Regex.Replace(normalized, "[^a-z0-9]+", "-").Trim('-');
    }
}
