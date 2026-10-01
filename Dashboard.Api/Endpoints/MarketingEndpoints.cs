using System.Text.RegularExpressions;
using Dashboard.Api.Contracts;
using Dashboard.Api.Data;
using Dashboard.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Endpoints;

public static class MarketingEndpoints
{
    private static readonly HashSet<string> CampaignStatuses = ["draft", "active", "paused", "completed"];
    private static readonly HashSet<string> PostStatuses = ["draft", "scheduled", "published", "archived"];
    private static readonly HashSet<string> FunnelStages = ["visit", "interest", "contact", "quote"];

    public static IEndpointRouteBuilder MapMarketingEndpoints(this IEndpointRouteBuilder app)
    {
        var marketing = app.MapGroup("/api/projects/{projectId:guid}/marketing").RequireAuthorization();

        marketing.MapGet("/campaigns", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var items = (await db.Campaigns.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(ct))
                .OrderByDescending(item => item.UpdatedAt);
            return Results.Ok(items.Select(CampaignResponse.FromEntity));
        });

        marketing.MapPost("/campaigns", async (Guid projectId, CampaignRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var errors = ValidateCampaign(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var now = DateTimeOffset.UtcNow;
            var item = new Campaign
            {
                Id = Guid.NewGuid(), ProjectId = projectId, Name = request.Name.Trim(),
                Objective = request.Objective.Trim(), Status = request.Status.Trim().ToLowerInvariant(),
                UtmCampaign = NormalizeUtm(request.UtmCampaign), StartDate = request.StartDate,
                EndDate = request.EndDate, IsDemoData = false, CreatedAt = now, UpdatedAt = now,
            };
            db.Campaigns.Add(item);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{projectId}/marketing/campaigns/{item.Id}", CampaignResponse.FromEntity(item));
        });

        marketing.MapPut("/campaigns/{campaignId:guid}", async (Guid projectId, Guid campaignId, CampaignRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var errors = ValidateCampaign(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var item = await db.Campaigns.SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Id == campaignId, ct);
            if (item is null) return Results.NotFound();
            item.Name = request.Name.Trim(); item.Objective = request.Objective.Trim();
            item.Status = request.Status.Trim().ToLowerInvariant(); item.UtmCampaign = NormalizeUtm(request.UtmCampaign);
            item.StartDate = request.StartDate; item.EndDate = request.EndDate; item.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(CampaignResponse.FromEntity(item));
        });

        marketing.MapDelete("/campaigns/{campaignId:guid}", async (Guid projectId, Guid campaignId, DashboardDbContext db, CancellationToken ct) =>
        {
            var item = await db.Campaigns.SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Id == campaignId, ct);
            if (item is null) return Results.NotFound();
            db.Campaigns.Remove(item);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        marketing.MapGet("/posts", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var items = (await db.SocialPosts.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(ct))
                .OrderByDescending(item => item.PublishedAt ?? item.CreatedAt);
            return Results.Ok(items.Select(SocialPostResponse.FromEntity));
        });

        marketing.MapPost("/posts", async (Guid projectId, SocialPostRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var errors = ValidatePost(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            if (!await CampaignBelongsToProject(request.CampaignId, projectId, db, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["campaignId"] = ["La campaña no pertenece a este proyecto."] });
            var now = DateTimeOffset.UtcNow;
            var item = new SocialPost
            {
                Id = Guid.NewGuid(), ProjectId = projectId, CampaignId = request.CampaignId,
                Platform = request.Platform.Trim().ToLowerInvariant(), Topic = request.Topic.Trim(),
                Format = request.Format.Trim().ToLowerInvariant(), Status = request.Status.Trim().ToLowerInvariant(),
                ExternalUrl = request.ExternalUrl?.Trim() ?? string.Empty, DataSource = "manual", PublishedAt = request.PublishedAt,
                Impressions = request.Impressions, Engagements = request.Engagements, Clicks = request.Clicks,
                CreatedAt = now, UpdatedAt = now,
            };
            db.SocialPosts.Add(item);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{projectId}/marketing/posts/{item.Id}", SocialPostResponse.FromEntity(item));
        });

        marketing.MapPut("/posts/{postId:guid}", async (Guid projectId, Guid postId, SocialPostRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            var errors = ValidatePost(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            if (!await CampaignBelongsToProject(request.CampaignId, projectId, db, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["campaignId"] = ["La campaña no pertenece a este proyecto."] });
            var item = await db.SocialPosts.SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Id == postId, ct);
            if (item is null) return Results.NotFound();
            item.CampaignId = request.CampaignId; item.Platform = request.Platform.Trim().ToLowerInvariant();
            item.Topic = request.Topic.Trim(); item.Format = request.Format.Trim().ToLowerInvariant();
            item.Status = request.Status.Trim().ToLowerInvariant(); item.ExternalUrl = request.ExternalUrl?.Trim() ?? string.Empty;
            item.PublishedAt = request.PublishedAt; item.Impressions = request.Impressions;
            item.Engagements = request.Engagements; item.Clicks = request.Clicks; item.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(SocialPostResponse.FromEntity(item));
        });

        marketing.MapDelete("/posts/{postId:guid}", async (Guid projectId, Guid postId, DashboardDbContext db, CancellationToken ct) =>
        {
            var item = await db.SocialPosts.SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Id == postId, ct);
            if (item is null) return Results.NotFound();
            db.SocialPosts.Remove(item);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        marketing.MapPost("/events/import", async (Guid projectId, MarketingImportRequest request, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var errors = await ValidateImport(request, projectId, db, ct);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var now = DateTimeOffset.UtcNow;
            var events = request.Events.Select(item => new MarketingEvent
            {
                Id = Guid.NewGuid(), ProjectId = projectId, CampaignId = item.CampaignId, SocialPostId = item.SocialPostId,
                Stage = item.Stage.Trim().ToLowerInvariant(), Source = item.Source.Trim().ToLowerInvariant(),
                Medium = item.Medium?.Trim().ToLowerInvariant() ?? "unknown", LandingPath = item.LandingPath?.Trim() ?? string.Empty,
                Count = item.Count, OccurredAt = item.OccurredAt ?? now,
                DataSource = NormalizeDataSource(item.DataSource), CreatedAt = now,
            }).ToList();
            db.MarketingEvents.AddRange(events);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { imported = events.Count, totalCount = events.Sum(item => item.Count) });
        });

        marketing.MapGet("/summary", async (Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        {
            if (!await ProjectExists(projectId, db, ct)) return Results.NotFound();
            var events = await db.MarketingEvents.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(ct);
            var posts = await db.SocialPosts.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(ct);
            var campaignCount = await db.Campaigns.CountAsync(item => item.ProjectId == projectId, ct);
            var funnel = new[] { "visit", "interest", "contact", "quote" }
                .Select(stage => new FunnelStageResponse(stage, events.Where(item => item.Stage == stage).Sum(item => item.Count))).ToList();
            var sources = events.GroupBy(item => item.Source).Select(group => new SourceMetricResponse(
                    group.Key,
                    group.Where(item => item.Stage == "visit").Sum(item => item.Count),
                    group.Where(item => item.Stage == "contact").Sum(item => item.Count)))
                .OrderByDescending(item => item.Visits).ToList();
            return Results.Ok(new MarketingSummaryResponse(
                funnel, sources, campaignCount, posts.Count, posts.Sum(item => item.Impressions),
                posts.Sum(item => item.Engagements), posts.Sum(item => item.Clicks),
                events.Any(item => item.DataSource == "simulated") || posts.Any(item => item.DataSource == "simulated")));
        });

        return app;
    }

    private static async Task<bool> ProjectExists(Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        await db.Projects.AnyAsync(item => item.Id == projectId, ct);

    private static async Task<bool> CampaignBelongsToProject(Guid? campaignId, Guid projectId, DashboardDbContext db, CancellationToken ct) =>
        campaignId is null || await db.Campaigns.AnyAsync(item => item.Id == campaignId && item.ProjectId == projectId, ct);

    private static Dictionary<string, string[]> ValidateCampaign(CampaignRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 140) errors["name"] = ["El nombre es obligatorio y admite hasta 140 caracteres."];
        if (string.IsNullOrWhiteSpace(request.Objective) || request.Objective.Trim().Length > 500) errors["objective"] = ["El objetivo es obligatorio y admite hasta 500 caracteres."];
        if (!CampaignStatuses.Contains(request.Status?.Trim().ToLowerInvariant() ?? string.Empty)) errors["status"] = ["El estado de campaña no es válido."];
        if (string.IsNullOrWhiteSpace(request.UtmCampaign) || NormalizeUtm(request.UtmCampaign).Length > 120) errors["utmCampaign"] = ["La etiqueta UTM de campaña es obligatoria y admite hasta 120 caracteres."];
        if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate < request.StartDate) errors["endDate"] = ["La fecha final no puede ser anterior a la fecha inicial."];
        return errors;
    }

    private static Dictionary<string, string[]> ValidatePost(SocialPostRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Platform) || request.Platform.Trim().Length > 40) errors["platform"] = ["La plataforma es obligatoria."];
        if (string.IsNullOrWhiteSpace(request.Topic) || request.Topic.Trim().Length > 220) errors["topic"] = ["El tema es obligatorio y admite hasta 220 caracteres."];
        if (string.IsNullOrWhiteSpace(request.Format) || request.Format.Trim().Length > 50) errors["format"] = ["El formato es obligatorio."];
        if (!PostStatuses.Contains(request.Status?.Trim().ToLowerInvariant() ?? string.Empty)) errors["status"] = ["El estado de publicación no es válido."];
        if ((request.ExternalUrl?.Length ?? 0) > 1000) errors["externalUrl"] = ["La URL admite hasta 1000 caracteres."];
        if (request.Impressions < 0 || request.Engagements < 0 || request.Clicks < 0) errors["metrics"] = ["Las métricas no pueden ser negativas."];
        return errors;
    }

    private static async Task<Dictionary<string, string[]>> ValidateImport(MarketingImportRequest request, Guid projectId, DashboardDbContext db, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.Events is null || request.Events.Count is < 1 or > 500)
        {
            errors["events"] = ["La importación debe contener entre 1 y 500 filas."];
            return errors;
        }
        for (var index = 0; index < request.Events.Count; index++)
        {
            var item = request.Events[index];
            if (!FunnelStages.Contains(item.Stage?.Trim().ToLowerInvariant() ?? string.Empty)) errors[$"events[{index}].stage"] = ["La etapa debe ser visit, interest, contact o quote."];
            if (string.IsNullOrWhiteSpace(item.Source) || item.Source.Trim().Length > 80) errors[$"events[{index}].source"] = ["La fuente es obligatoria y admite hasta 80 caracteres."];
            if (item.Count is < 1 or > 1_000_000) errors[$"events[{index}].count"] = ["El conteo debe estar entre 1 y 1,000,000."];
            if (item.CampaignId.HasValue && !await CampaignBelongsToProject(item.CampaignId, projectId, db, ct)) errors[$"events[{index}].campaignId"] = ["La campaña no pertenece a este proyecto."];
            if (item.SocialPostId.HasValue && !await db.SocialPosts.AnyAsync(post => post.Id == item.SocialPostId && post.ProjectId == projectId, ct)) errors[$"events[{index}].socialPostId"] = ["La publicación no pertenece a este proyecto."];
        }
        return errors;
    }

    private static string NormalizeUtm(string value) => Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9_-]+", "-").Trim('-');
    private static string NormalizeDataSource(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "import_csv" => "import_csv",
        "import_json" => "import_json",
        "simulated" => "simulated",
        _ => "manual",
    };
}
