using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Dashboard.Api.Contracts;
using Dashboard.Api.Data;
using Dashboard.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Services;

public sealed class PostHogService(DashboardDbContext db, IPostHogApiClient client, IConfiguration configuration)
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> SyncLocks = new();
    private static readonly Regex ApiKeyName = new("^POSTHOG_PERSONAL_API_KEY(?:_[A-Z0-9_]+)?$", RegexOptions.Compiled);
    private static readonly Regex SafeTag = new("^[a-zA-Z0-9_-]{1,160}$", RegexOptions.Compiled);
    private static readonly Regex SafePath = new("^/[a-zA-Z0-9_./-]{0,299}$", RegexOptions.Compiled);

    public static Dictionary<string, string[]> Validate(PostHogSettingsRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.Region is not ("us" or "eu")) errors["region"] = ["Selecciona us o eu."];
        if (request.ExternalProjectId <= 0) errors["externalProjectId"] = ["Indica el ID numérico del proyecto de PostHog."];
        if (string.IsNullOrWhiteSpace(request.PublicToken) || !Regex.IsMatch(request.PublicToken, "^phc_[a-zA-Z0-9]{8,156}$"))
            errors["publicToken"] = ["Indica el token público del proyecto (phc_...)."];
        if (string.IsNullOrWhiteSpace(request.ApiKeyEnvironmentVariable) || !ApiKeyName.IsMatch(request.ApiKeyEnvironmentVariable))
            errors["apiKeyEnvironmentVariable"] = ["La variable debe comenzar con POSTHOG_PERSONAL_API_KEY."];
        if (request.LookbackDays is < 1 or > 31) errors["lookbackDays"] = ["El rango debe estar entre 1 y 31 días."];
        if (request.RowLimit is < 100 or > 5000) errors["rowLimit"] = ["El máximo de filas debe estar entre 100 y 5,000."];
        return errors;
    }

    public async Task<PostHogStatusResponse> GetStatusAsync(Guid projectId, CancellationToken ct)
    {
        var item = await db.PostHogConnections.AsNoTracking().SingleOrDefaultAsync(value => value.ProjectId == projectId, ct);
        return PostHogStatusResponse.FromEntity(item, item is not null && !string.IsNullOrWhiteSpace(configuration[item.ApiKeyEnvironmentVariable]));
    }

    public async Task<PostHogStatusResponse> SaveSettingsAsync(Guid projectId, PostHogSettingsRequest request, CancellationToken ct)
    {
        var item = await db.PostHogConnections.SingleOrDefaultAsync(value => value.ProjectId == projectId, ct);
        var now = DateTimeOffset.UtcNow;
        if (item is null)
        {
            item = new PostHogConnection
            {
                Id = Guid.NewGuid(), ProjectId = projectId, Region = request.Region,
                ExternalProjectId = request.ExternalProjectId, PublicToken = request.PublicToken.Trim(),
                ApiKeyEnvironmentVariable = request.ApiKeyEnvironmentVariable.Trim(),
                LookbackDays = request.LookbackDays, RowLimit = request.RowLimit, IsEnabled = request.IsEnabled,
                LastError = string.Empty, CreatedAt = now, UpdatedAt = now,
            };
            db.PostHogConnections.Add(item);
        }
        else
        {
            if (item.ExternalProjectId != request.ExternalProjectId || item.Region != request.Region)
            {
                // Metrics from another PostHog project cannot be presented as this connection's current data.
                db.PostHogMetrics.RemoveRange(await db.PostHogMetrics.Where(value => value.ProjectId == projectId).ToListAsync(ct));
                item.LastSyncAt = null;
            }
            item.Region = request.Region;
            item.ExternalProjectId = request.ExternalProjectId;
            item.PublicToken = request.PublicToken.Trim();
            item.ApiKeyEnvironmentVariable = request.ApiKeyEnvironmentVariable.Trim();
            item.LookbackDays = request.LookbackDays;
            item.RowLimit = request.RowLimit;
            item.IsEnabled = request.IsEnabled;
            item.LastError = string.Empty;
            item.UpdatedAt = now;
        }
        var project = await db.Projects.SingleAsync(value => value.Id == projectId, ct);
        var schedule = await db.AutomationSchedules.SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Workflow == "posthog_sync", ct);
        if (schedule is null)
        {
            schedule = new AutomationSchedule
            {
                Id = Guid.NewGuid(), ProjectId = projectId, Workflow = "posthog_sync",
                DisplayName = "Sincronización de analítica PostHog", IsEnabled = request.IsEnabled,
                Frequency = "daily", LocalTime = "07:00", DayOfWeek = null,
                IntervalMinutes = null, MaxRunsPerDay = 3, UpdatedAt = now,
            };
            schedule.NextRunAt = AutomationScheduleCalculator.CalculateNext(schedule, project.TimeZone, now);
            db.AutomationSchedules.Add(schedule);
        }
        else if (!request.IsEnabled)
        {
            schedule.IsEnabled = false;
            schedule.NextRunAt = null;
        }
        else if (!schedule.IsEnabled)
        {
            schedule.IsEnabled = true;
            schedule.Frequency = "daily";
            schedule.NextRunAt = AutomationScheduleCalculator.CalculateNext(schedule, project.TimeZone, now);
        }
        await db.SaveChangesAsync(ct);
        return await GetStatusAsync(projectId, ct);
    }

    public async Task<PostHogSyncResult> SyncAsync(Guid projectId, CancellationToken ct)
    {
        var gate = SyncLocks.GetOrAdd(projectId, _ => new SemaphoreSlim(1, 1));
        if (!await gate.WaitAsync(0, ct))
            return Failed("POSTHOG_ALREADY_RUNNING", "Ya hay una sincronización de PostHog en curso para este proyecto.");
        try { return await SyncCoreAsync(projectId, ct); }
        finally { gate.Release(); }
    }

    private async Task<PostHogSyncResult> SyncCoreAsync(Guid projectId, CancellationToken ct)
    {
        var item = await db.PostHogConnections.SingleOrDefaultAsync(value => value.ProjectId == projectId, ct);
        if (item is null || !item.IsEnabled)
            return Failed("POSTHOG_NOT_CONNECTED", "Configura y activa PostHog para este proyecto.");
        var personalKey = configuration[item.ApiKeyEnvironmentVariable];
        if (string.IsNullOrWhiteSpace(personalKey))
            return Failed("POSTHOG_KEY_NOT_CONFIGURED", $"Falta {item.ApiKeyEnvironmentVariable} en .env. Reinicia la API tras añadirla.");

        try
        {
            var endExclusive = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));
            var start = endExclusive.AddDays(-item.LookbackDays);
            var queried = await client.QueryAsync(item.Region, item.ExternalProjectId, personalKey, start, endExclusive, item.RowLimit, ct);
            if (queried.Count > item.RowLimit * 2 + item.LookbackDays)
                throw new PostHogApiException("POSTHOG_ROW_LIMIT", "La consulta supera el máximo de filas. Aumenta el límite o reduce los días a importar; no se guardó un resultado parcial.");

            var rows = queried.Where(value => value.Date >= start && value.Date < endExclusive &&
                value.Event is ("$pageview" or "fyr_quote_request" or "fyr_whatsapp_click" or "$unique_sessions" or "$utm_sessions") && value.EventCount >= 0 && value.Sessions >= 0)
                .Select(value => new
                {
                    value.Date, value.Event, Source = Tag(value.Source, "unattributed"), Medium = Tag(value.Medium, "unknown"),
                    Campaign = Tag(value.Campaign), Content = Tag(value.Content), Path = Path(value.Path),
                    value.EventCount, value.Sessions,
                })
                .GroupBy(value => new { value.Date, value.Event, value.Source, value.Medium, value.Campaign, value.Content, value.Path })
                .Select(group => new { group.Key, EventCount = group.Sum(value => value.EventCount), Sessions = group.Sum(value => value.Sessions) })
                .ToList();
            var existing = await db.PostHogMetrics.Where(value => value.ProjectId == projectId && value.Date >= start && value.Date < endExclusive).ToListAsync(ct);
            db.PostHogMetrics.RemoveRange(existing);
            var now = DateTimeOffset.UtcNow;
            db.PostHogMetrics.AddRange(rows.Select(value => new PostHogMetric
            {
                Id = Guid.NewGuid(), ProjectId = projectId, PostHogConnectionId = item.Id,
                Date = value.Key.Date, Event = value.Key.Event, Source = value.Key.Source,
                Medium = value.Key.Medium, Campaign = value.Key.Campaign, Content = value.Key.Content,
                Path = value.Key.Path, EventCount = value.EventCount, Sessions = value.Sessions, SyncedAt = now,
            }));
            item.LastSyncAt = now;
            item.LastError = string.Empty;
            item.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
            return new PostHogSyncResult(true, string.Empty, string.Empty, rows.Count,
                rows.Where(value => value.Key.Event == "$pageview").Sum(value => value.EventCount),
                rows.Where(value => value.Key.Event == "fyr_quote_request").Sum(value => value.EventCount),
                rows.Where(value => value.Key.Event == "fyr_whatsapp_click").Sum(value => value.EventCount));
        }
        catch (PostHogApiException exception)
        {
            item.LastError = exception.Message;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Failed(exception.Code, exception.Message);
        }
    }

    public async Task<PostHogSummaryResponse> GetSummaryAsync(Guid projectId, int days, CancellationToken ct)
    {
        var start = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-(days - 1)));
        var metrics = await db.PostHogMetrics.AsNoTracking().Where(value => value.ProjectId == projectId && value.Date >= start)
            .OrderByDescending(value => value.Date).ThenByDescending(value => value.EventCount).ToListAsync(ct);
        var proposals = await db.XReplyProposals.AsNoTracking().Where(value => value.ProjectId == projectId)
            .Select(value => new { value.Id, value.TrackingUrl }).ToListAsync(ct);
        var replyIds = proposals.Select(value => new { value.Id, Tag = TrackingContent(value.TrackingUrl) })
            .Where(value => value.Tag.Length > 0).GroupBy(value => value.Tag).ToDictionary(value => value.Key, value => value.First().Id);
        var campaigns = await db.Campaigns.AsNoTracking().Where(value => value.ProjectId == projectId)
            .Select(value => new { value.Id, value.UtmCampaign }).ToListAsync(ct);
        var campaignIds = campaigns.GroupBy(value => value.UtmCampaign, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(value => value.Key, value => value.First().Id, StringComparer.OrdinalIgnoreCase);
        var connection = await db.PostHogConnections.AsNoTracking().SingleOrDefaultAsync(value => value.ProjectId == projectId, ct);
        var attribution = metrics.Where(value => value.Event != "$unique_sessions" && (value.Campaign.Length > 0 || value.Content.Length > 0))
            .GroupBy(value => new { value.Source, value.Campaign, value.Content })
            .Select(group => new PostHogAttributionResponse(group.Key.Source, group.Key.Campaign, group.Key.Content,
                group.Where(value => value.Event == "$utm_sessions").Sum(value => value.EventCount),
                group.Where(value => value.Event == "$pageview").Sum(value => value.EventCount),
                group.Where(value => value.Event == "fyr_quote_request").Sum(value => value.EventCount),
                group.Where(value => value.Event == "fyr_whatsapp_click").Sum(value => value.EventCount),
                replyIds.TryGetValue(group.Key.Content, out var proposalId) ? proposalId : null,
                campaignIds.TryGetValue(group.Key.Campaign, out var campaignId) ? campaignId : null))
            .OrderByDescending(value => value.Pageviews).Take(100).ToList();
        return new PostHogSummaryResponse(
            metrics.Where(value => value.Event == "$pageview").Sum(value => value.EventCount),
            metrics.Where(value => value.Event == "$unique_sessions").Sum(value => value.EventCount),
            metrics.Where(value => value.Event == "fyr_quote_request").Sum(value => value.EventCount),
            metrics.Where(value => value.Event == "fyr_whatsapp_click").Sum(value => value.EventCount),
            metrics.Where(value => value.Event is not ("$unique_sessions" or "$utm_sessions")).Take(300).Select(value => new PostHogMetricResponse(value.Date, value.Event, value.Source, value.Medium,
                value.Campaign, value.Content, value.Path, value.EventCount, value.Sessions,
                replyIds.TryGetValue(value.Content, out var proposalId) ? proposalId : null,
                campaignIds.TryGetValue(value.Campaign, out var campaignId) ? campaignId : null)).ToList(),
            attribution,
            connection?.LastSyncAt);
    }

    public async Task<IReadOnlyList<PostHogEventDetailResponse>> GetRecentEventsAsync(
        Guid projectId, int days, string? source, string? campaign, string? content, CancellationToken ct)
    {
        if (days is < 1 or > 90 || new[] { source, campaign, content }.Any(value =>
            !string.IsNullOrWhiteSpace(value) && !SafeTag.IsMatch(value)))
            throw new PostHogApiException("POSTHOG_INVALID_CONFIGURATION", "Periodo o filtro UTM no válido.");
        var connection = await db.PostHogConnections.AsNoTracking().SingleOrDefaultAsync(value => value.ProjectId == projectId, ct);
        if (connection is null || !connection.IsEnabled)
            throw new PostHogApiException("POSTHOG_NOT_CONNECTED", "PostHog no está activo para este proyecto.");
        var personalKey = configuration[connection.ApiKeyEnvironmentVariable];
        if (string.IsNullOrWhiteSpace(personalKey))
            throw new PostHogApiException("POSTHOG_KEY_NOT_CONFIGURED", "Falta la clave de consulta de PostHog en .env.");
        var endExclusive = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));
        var start = endExclusive.AddDays(-days);
        var events = await client.QueryRecentEventsAsync(connection.Region, connection.ExternalProjectId, personalKey,
            start, endExclusive, source, campaign, content, 30, ct);
        return events.Select(value =>
        {
            var utmSource = Tag(value.Source);
            var referrer = ReferrerDomain(value.ReferrerDomain);
            var attribution = utmSource.Length > 0 ? "utm" : referrer == "$direct" ? "direct" :
                referrer.Length > 0 ? "referrer" : "unknown";
            return value with
            {
                Event = value.Event is ("$pageview" or "fyr_quote_request" or "fyr_whatsapp_click") ? value.Event : "unknown",
                Path = Path(value.Path),
                Source = utmSource.Length > 0 ? utmSource : SocialSource(referrer),
                Medium = Tag(value.Medium), Campaign = Tag(value.Campaign), Content = Tag(value.Content),
                Attribution = attribution, ReferrerDomain = referrer,
                Browser = Technical(value.Browser), City = Technical(value.City), Country = Technical(value.Country),
                DeviceType = Technical(value.DeviceType), Os = Technical(value.Os), OsVersion = Technical(value.OsVersion),
            };
        }).ToList();
    }

    private static string Tag(string? value, string fallback = "") =>
        !string.IsNullOrWhiteSpace(value) && SafeTag.IsMatch(value.Trim()) ? value.Trim().ToLowerInvariant() : fallback;

    private static string Technical(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return new string(value.Trim().Where(character => !char.IsControl(character)).Take(80).ToArray());
    }

    private static string ReferrerDomain(string? value)
    {
        if (value == "$direct") return "$direct";
        var domain = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return domain.Length <= 253 && Regex.IsMatch(domain, "^[a-z0-9.-]+$") ? domain : string.Empty;
    }

    private static string SocialSource(string referrer) => referrer switch
    {
        "$direct" => "direct",
        "x.com" or "t.co" or "twitter.com" or "www.x.com" or "www.twitter.com" => "x",
        "instagram.com" or "www.instagram.com" or "l.instagram.com" => "instagram",
        "facebook.com" or "www.facebook.com" or "l.facebook.com" or "m.facebook.com" => "facebook",
        "linkedin.com" or "www.linkedin.com" or "lnkd.in" => "linkedin",
        _ => referrer.Length > 0 ? referrer : "unattributed",
    };

    private static string Path(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !SafePath.IsMatch(value.Trim())) return "/";
        var path = value.Trim();
        path = Regex.Replace(path, @"(?<=/)[0-9]+(?=/|$)", ":id");
        return Regex.Replace(path, @"(?<=/)[0-9a-f]{8}-[0-9a-f-]{27,}(?=/|$)", ":id", RegexOptions.IgnoreCase);
    }

    private static string TrackingContent(string trackingUrl)
    {
        if (!Uri.TryCreate(trackingUrl, UriKind.Absolute, out var uri)) return string.Empty;
        var part = uri.Query.TrimStart('?').Split('&').FirstOrDefault(value => value.StartsWith("utm_content=", StringComparison.OrdinalIgnoreCase));
        return part is null ? string.Empty : Uri.UnescapeDataString(part[(part.IndexOf('=') + 1)..]).ToLowerInvariant();
    }

    private static PostHogSyncResult Failed(string code, string message) => new(false, code, message, 0, 0, 0, 0);
}
