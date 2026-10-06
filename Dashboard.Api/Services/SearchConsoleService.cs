using System.Globalization;
using System.Security.Cryptography;
using Dashboard.Api.Contracts;
using Dashboard.Api.Data;
using Dashboard.Api.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Services;

public sealed class SearchConsoleService(
    DashboardDbContext db,
    ISearchConsoleApiClient apiClient,
    IDataProtectionProvider dataProtectionProvider,
    IConfiguration configuration)
{
    public const string Provider = "google_search_console";
    public const string ReadOnlyScope = "https://www.googleapis.com/auth/webmasters.readonly";
    private readonly IDataProtector _tokenProtector = dataProtectionProvider.CreateProtector("Dashboard.SearchConsole.RefreshToken.v1");

    public bool ClientConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
    private string ClientId => configuration["GOOGLE_SEARCH_CONSOLE_CLIENT_ID"]?.Trim() ?? string.Empty;
    private string ClientSecret => configuration["GOOGLE_SEARCH_CONSOLE_CLIENT_SECRET"]?.Trim() ?? string.Empty;
    public string WebBaseUrl
    {
        get
        {
            var configured = configuration["DASHBOARD_WEB_BASE_URL"]?.Trim().TrimEnd('/');
            return string.IsNullOrWhiteSpace(configured) ? "http://localhost:5173" : configured;
        }
    }

    public async Task<SearchConsoleStatusResponse> GetStatusAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var item = await db.IntegrationConnections.AsNoTracking()
            .SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Provider == Provider, cancellationToken);
        return item is null
            ? new SearchConsoleStatusResponse(ClientConfigured, false, "not_connected", string.Empty, string.Empty, 28, 1000, null, string.Empty)
            : new SearchConsoleStatusResponse(ClientConfigured, item.Status == "connected", item.Status, item.ResourceId,
                item.ExternalAccount, item.LookbackDays, item.RowLimit, item.LastSyncAt, item.LastError);
    }

    public async Task<string> CreateAuthorizationUrlAsync(Guid projectId, string redirectUri, CancellationToken cancellationToken)
    {
        if (!ClientConfigured) throw new InvalidOperationException("Configura GOOGLE_SEARCH_CONSOLE_CLIENT_ID y GOOGLE_SEARCH_CONSOLE_CLIENT_SECRET en .env.");
        if (!await db.Projects.AnyAsync(item => item.Id == projectId, cancellationToken)) throw new KeyNotFoundException();

        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;
        db.OAuthStates.Add(new OAuthStateRecord
        {
            Id = Guid.NewGuid(), ProjectId = projectId, Provider = Provider, StateHash = HashState(state),
            CreatedAt = now, ExpiresAt = now.AddMinutes(10),
        });
        await db.SaveChangesAsync(cancellationToken);

        var values = new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = ReadOnlyScope,
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["include_granted_scopes"] = "true",
            ["state"] = state,
        };
        return "https://accounts.google.com/o/oauth2/v2/auth?" + string.Join('&', values.Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}"));
    }

    public async Task<Guid> CompleteAuthorizationAsync(string code, string state, string redirectUri, CancellationToken cancellationToken)
    {
        var stateHash = HashState(state);
        var oauthState = await db.OAuthStates.SingleOrDefaultAsync(item => item.StateHash == stateHash && item.Provider == Provider, cancellationToken)
            ?? throw new InvalidOperationException("El estado de autorización no es válido.");
        if (oauthState.UsedAt.HasValue || oauthState.ExpiresAt < DateTimeOffset.UtcNow)
            throw new InvalidOperationException("La autorización expiró o ya fue utilizada.");
        oauthState.UsedAt = DateTimeOffset.UtcNow;

        var project = await db.Projects.SingleAsync(item => item.Id == oauthState.ProjectId, cancellationToken);
        var existing = await db.IntegrationConnections.SingleOrDefaultAsync(item => item.ProjectId == project.Id && item.Provider == Provider, cancellationToken);
        var token = await apiClient.ExchangeCodeAsync(code, redirectUri, ClientId, ClientSecret, cancellationToken);
        var sites = await apiClient.ListSitesAsync(token.AccessToken, cancellationToken);
        var site = SelectSite(sites, project.Domain)
            ?? throw new InvalidOperationException("La cuenta autorizada no tiene acceso verificado a una propiedad compatible con el dominio del proyecto.");
        var refreshToken = token.RefreshToken;
        if (string.IsNullOrWhiteSpace(refreshToken) && existing is null)
            throw new InvalidOperationException("Google no devolvió un token de actualización. Revoca el permiso anterior e intenta conectar de nuevo.");

        var now = DateTimeOffset.UtcNow;
        var connection = existing ?? new IntegrationConnection
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, Provider = Provider, Status = "connected",
            ExternalAccount = site.PermissionLevel, ResourceId = site.SiteUrl, EncryptedRefreshToken = string.Empty,
            GrantedScopes = ReadOnlyScope, LookbackDays = 28, RowLimit = 1000, LastError = string.Empty,
            CreatedAt = now, UpdatedAt = now,
        };
        connection.Status = "connected";
        connection.ExternalAccount = site.PermissionLevel;
        connection.ResourceId = site.SiteUrl;
        connection.GrantedScopes = token.Scope ?? ReadOnlyScope;
        connection.LastError = string.Empty;
        connection.UpdatedAt = now;
        if (!string.IsNullOrWhiteSpace(refreshToken)) connection.EncryptedRefreshToken = _tokenProtector.Protect(refreshToken);
        if (existing is null) db.IntegrationConnections.Add(connection);

        var schedule = await db.AutomationSchedules.SingleOrDefaultAsync(item => item.ProjectId == project.Id && item.Workflow == "search_console_sync", cancellationToken);
        if (schedule is not null)
        {
            schedule.IsEnabled = true;
            schedule.Frequency = "daily";
            schedule.NextRunAt = AutomationScheduleCalculator.CalculateNext(schedule, project.TimeZone, now);
            schedule.UpdatedAt = now;
        }
        await db.SaveChangesAsync(cancellationToken);
        return project.Id;
    }

    public async Task<SearchConsoleStatusResponse?> UpdateSettingsAsync(Guid projectId, SearchConsoleSettingsRequest request, CancellationToken cancellationToken)
    {
        var item = await db.IntegrationConnections.SingleOrDefaultAsync(value => value.ProjectId == projectId && value.Provider == Provider, cancellationToken);
        if (item is null) return null;
        item.LookbackDays = request.LookbackDays;
        item.RowLimit = request.RowLimit;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return await GetStatusAsync(projectId, cancellationToken);
    }

    public async Task<SearchConsoleSyncResult> SyncAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (!ClientConfigured) return Failed("SEARCH_CONSOLE_CLIENT_NOT_CONFIGURED", "Faltan las credenciales OAuth de Search Console.");
        var connection = await db.IntegrationConnections.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Provider == Provider, cancellationToken);
        if (connection is null || connection.Status != "connected") return Failed("SEARCH_CONSOLE_NOT_CONNECTED", "Search Console todavía no está conectado para este proyecto.");

        try
        {
            var refreshToken = _tokenProtector.Unprotect(connection.EncryptedRefreshToken);
            var token = await apiClient.RefreshAccessTokenAsync(refreshToken, ClientId, ClientSecret, cancellationToken);
            var endDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-2));
            var startDate = endDate.AddDays(-(connection.LookbackDays - 1));
            var rows = await apiClient.QueryAsync(token.AccessToken, connection.ResourceId, startDate, endDate, connection.RowLimit, cancellationToken);
            var existing = await db.SearchConsoleMetrics.Where(item => item.IntegrationConnectionId == connection.Id).ToListAsync(cancellationToken);
            db.SearchConsoleMetrics.RemoveRange(existing.Where(item => item.Date >= startDate && item.Date <= endDate));
            var now = DateTimeOffset.UtcNow;
            foreach (var row in rows.Where(item => item.Keys.Count >= 3))
            {
                if (!DateOnly.TryParseExact(row.Keys[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) continue;
                db.SearchConsoleMetrics.Add(new SearchConsoleMetric
                {
                    Id = Guid.NewGuid(), ProjectId = projectId, IntegrationConnectionId = connection.Id,
                    Date = date, Query = row.Keys[1], Page = row.Keys[2], Clicks = row.Clicks,
                    Impressions = row.Impressions, Ctr = row.Ctr, Position = row.Position, SyncedAt = now,
                });
            }
            connection.LastSyncAt = now;
            connection.LastError = string.Empty;
            connection.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            return new SearchConsoleSyncResult(true, string.Empty, string.Empty, rows.Count,
                rows.Sum(item => item.Clicks), rows.Sum(item => item.Impressions), startDate, endDate);
        }
        catch (SearchConsoleApiException exception)
        {
            if (exception.Code == "SEARCH_CONSOLE_AUTH_FAILED") connection.Status = "reauthorization_required";
            connection.LastError = exception.Message;
            connection.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return Failed(exception.Code, exception.Message);
        }
        catch (HttpRequestException)
        {
            const string message = "No se pudo contactar a Google Search Console. Comprueba la conexión e inténtalo de nuevo.";
            connection.LastError = message;
            connection.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return Failed("SEARCH_CONSOLE_UNAVAILABLE", message);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            const string message = "Google Search Console no respondió dentro del tiempo esperado.";
            connection.LastError = message;
            connection.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return Failed("SEARCH_CONSOLE_TIMEOUT", message);
        }
        catch (CryptographicException)
        {
            connection.Status = "reauthorization_required";
            connection.LastError = "No se pudo descifrar la autorización almacenada; vuelve a conectar Search Console.";
            connection.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return Failed("SEARCH_CONSOLE_REAUTHORIZATION_REQUIRED", connection.LastError);
        }
    }

    public async Task<SearchConsoleSummaryResponse> GetSummaryAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var items = await db.SearchConsoleMetrics.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(cancellationToken);
        if (items.Count == 0) return new SearchConsoleSummaryResponse(0, 0, 0, 0, 0, null, null, []);
        var impressions = items.Sum(item => item.Impressions);
        var grouped = items.GroupBy(item => new { item.Query, item.Page }).Select(group => new SearchConsoleQueryMetric(
            group.Key.Query, group.Key.Page, group.Sum(item => item.Clicks), group.Sum(item => item.Impressions),
            group.Sum(item => item.Impressions) == 0 ? 0 : group.Sum(item => item.Clicks) / group.Sum(item => item.Impressions),
            group.Sum(item => item.Impressions) == 0 ? group.Average(item => item.Position) : group.Sum(item => item.Position * item.Impressions) / group.Sum(item => item.Impressions)))
            .OrderByDescending(item => item.Impressions).Take(10).ToList();
        return new SearchConsoleSummaryResponse(items.Count, items.Sum(item => item.Clicks), impressions,
            impressions == 0 ? 0 : items.Sum(item => item.Clicks) / impressions,
            impressions == 0 ? items.Average(item => item.Position) : items.Sum(item => item.Position * item.Impressions) / impressions,
            items.Min(item => item.Date), items.Max(item => item.Date), grouped);
    }

    public async Task<bool> DisconnectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var connection = await db.IntegrationConnections.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Provider == Provider, cancellationToken);
        if (connection is null) return false;
        db.IntegrationConnections.Remove(connection);
        var schedule = await db.AutomationSchedules.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Workflow == "search_console_sync", cancellationToken);
        if (schedule is not null)
        {
            schedule.IsEnabled = false;
            schedule.NextRunAt = null;
            schedule.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static SearchConsoleSite? SelectSite(IReadOnlyList<SearchConsoleSite> sites, string projectDomain)
    {
        var domain = NormalizeDomain(projectDomain);
        return sites.Where(item => !item.PermissionLevel.Equals("siteUnverifiedUser", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.SiteUrl.Equals($"sc-domain:{domain}", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(item => SiteMatchesDomain(item.SiteUrl, domain))
            .FirstOrDefault(item => item.SiteUrl.Equals($"sc-domain:{domain}", StringComparison.OrdinalIgnoreCase) || SiteMatchesDomain(item.SiteUrl, domain));
    }

    private static string NormalizeDomain(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri)) return uri.Host.TrimStart('.').ToLowerInvariant();
        return value.Trim().TrimEnd('/').Replace("sc-domain:", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
    }

    private static bool SiteMatchesDomain(string siteUrl, string domain) =>
        Uri.TryCreate(siteUrl, UriKind.Absolute, out var uri) &&
        (uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase) || uri.Host.Equals($"www.{domain}", StringComparison.OrdinalIgnoreCase));

    private static string HashState(string state) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(state))).ToLowerInvariant();
    private static SearchConsoleSyncResult Failed(string code, string message) => new(false, code, message, 0, 0, 0, null, null);
}
