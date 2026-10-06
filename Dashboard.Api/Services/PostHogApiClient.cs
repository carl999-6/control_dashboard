using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dashboard.Api.Contracts;

namespace Dashboard.Api.Services;

public sealed class PostHogApiClient : IPostHogApiClient, IDisposable
{
    private static readonly Regex SafeFilter = new("^[a-zA-Z0-9_-]{1,160}$", RegexOptions.Compiled);
    private readonly HttpClient _httpClient;
    private readonly bool _ownsClient;

    public PostHogApiClient() : this(new HttpClient(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
    }) { Timeout = TimeSpan.FromSeconds(60) }, true) { }

    public PostHogApiClient(HttpClient httpClient) : this(httpClient, false) { }

    private PostHogApiClient(HttpClient httpClient, bool ownsClient)
    {
        _httpClient = httpClient;
        _ownsClient = ownsClient;
    }

    public async Task<IReadOnlyList<PostHogQueryRow>> QueryAsync(
        string region, int projectId, string personalApiKey, DateOnly startDate,
        DateOnly endDateExclusive, int rowLimit, CancellationToken cancellationToken)
    {
        if (region is not ("us" or "eu") || projectId <= 0)
            throw new PostHogApiException("POSTHOG_INVALID_CONFIGURATION", "La región o el proyecto de PostHog no son válidos.");

        // Fixed event names and validated dates: no user-provided SQL or remote URL is accepted.
        var sql = $"""
            SELECT toString(toDate(timestamp)) AS day, event,
              properties.utm_source AS source, properties.utm_medium AS medium,
              properties.utm_campaign AS campaign, properties.utm_content AS content,
              properties.$pathname AS path, count() AS event_count,
              uniqExact(properties.$session_id) AS sessions
            FROM events
            WHERE timestamp >= toDateTime('{startDate:yyyy-MM-dd} 00:00:00', 'UTC')
              AND timestamp < toDateTime('{endDateExclusive:yyyy-MM-dd} 00:00:00', 'UTC')
              AND event IN ('$pageview', 'fyr_quote_request', 'fyr_whatsapp_click')
            GROUP BY day, event, source, medium, campaign, content, path
            ORDER BY day, event, source, campaign, content, path
            LIMIT {rowLimit + 1}
            """;
        var rows = await RequestRowsAsync(region, projectId, personalApiKey, sql, "Dashboard 7F UTM segments", cancellationToken);
        if (rows.Count > rowLimit)
            throw new PostHogApiException("POSTHOG_ROW_LIMIT", "La consulta supera el máximo de filas. Reduce los días o aumenta el límite.");

        var sessionSql = $"""
            SELECT toString(toDate(timestamp)) AS day, '$unique_sessions' AS metric_event,
              '' AS source, '' AS medium, '' AS campaign, '' AS content, '' AS path,
              uniqExact(properties.$session_id) AS event_count, 0 AS sessions
            FROM events
            WHERE timestamp >= toDateTime('{startDate:yyyy-MM-dd} 00:00:00', 'UTC')
              AND timestamp < toDateTime('{endDateExclusive:yyyy-MM-dd} 00:00:00', 'UTC')
              AND event = '$pageview'
            GROUP BY day
            ORDER BY day
            LIMIT 32
            """;
        rows.AddRange(await RequestRowsAsync(region, projectId, personalApiKey, sessionSql, "Dashboard 7F daily sessions", cancellationToken));
        var utmSessionSql = $"""
            SELECT toString(toDate(timestamp)) AS day, '$utm_sessions' AS metric_event,
              properties.utm_source AS source, properties.utm_medium AS medium,
              properties.utm_campaign AS campaign, properties.utm_content AS content,
              '' AS path, uniqExact(properties.$session_id) AS event_count, 0 AS sessions
            FROM events
            WHERE timestamp >= toDateTime('{startDate:yyyy-MM-dd} 00:00:00', 'UTC')
              AND timestamp < toDateTime('{endDateExclusive:yyyy-MM-dd} 00:00:00', 'UTC')
              AND event = '$pageview'
              AND (properties.utm_campaign != '' OR properties.utm_content != '')
            GROUP BY day, source, medium, campaign, content
            ORDER BY day, source, campaign, content
            LIMIT {rowLimit + 1}
            """;
        var utmSessions = await RequestRowsAsync(region, projectId, personalApiKey, utmSessionSql, "Dashboard 7F UTM sessions", cancellationToken);
        if (utmSessions.Count > rowLimit)
            throw new PostHogApiException("POSTHOG_ROW_LIMIT", "Las sesiones UTM superan el máximo de filas. Reduce los días o aumenta el límite.");
        rows.AddRange(utmSessions);
        return rows;
    }

    public async Task<IReadOnlyList<PostHogEventDetailResponse>> QueryRecentEventsAsync(
        string region, int projectId, string personalApiKey, DateOnly startDate,
        DateOnly endDateExclusive, string? source, string? campaign, string? content,
        int limit, CancellationToken cancellationToken)
    {
        if (region is not ("us" or "eu") || projectId <= 0 || limit is < 1 or > 50)
            throw new PostHogApiException("POSTHOG_INVALID_CONFIGURATION", "La consulta de eventos no es válida.");
        var sourceFilter = Filter("utm_source", source);
        var campaignFilter = Filter("utm_campaign", campaign);
        var contentFilter = Filter("utm_content", content);
        var sql = $"""
            SELECT toUnixTimestamp(timestamp), event, properties.$pathname,
              properties.utm_source, properties.utm_medium, properties.utm_campaign,
              properties.utm_content, properties.$referring_domain, properties.$browser,
              properties.$geoip_city_name, properties.$geoip_country_name,
              properties.$device_type, properties.$os, properties.$os_version
            FROM events
            WHERE timestamp >= toDateTime('{startDate:yyyy-MM-dd} 00:00:00', 'UTC')
              AND timestamp < toDateTime('{endDateExclusive:yyyy-MM-dd} 00:00:00', 'UTC')
              AND event IN ('$pageview', 'fyr_quote_request', 'fyr_whatsapp_click')
              {sourceFilter}{campaignFilter}{contentFilter}
            ORDER BY timestamp DESC
            LIMIT {limit}
            """;
        var results = await RequestResultRowsAsync(region, projectId, personalApiKey, sql, "Dashboard 7F recent event details", cancellationToken);
        var events = new List<PostHogEventDetailResponse>(results.Count);
        foreach (var entry in results)
        {
            if (entry.ValueKind != JsonValueKind.Array || entry.GetArrayLength() != 14 ||
                !entry[0].TryGetInt64(out var seconds) || seconds is < 0 or > 32503680000)
                throw new PostHogApiException("POSTHOG_INVALID_RESPONSE", "PostHog devolvió un evento con formato inesperado.");
            events.Add(new PostHogEventDetailResponse(DateTimeOffset.FromUnixTimeSeconds(seconds),
                Value(entry[1]), Value(entry[2]), Value(entry[3]), Value(entry[4]),
                Value(entry[5]), Value(entry[6]), string.Empty, Value(entry[7]),
                Value(entry[8]), Value(entry[9]), Value(entry[10]), Value(entry[11]),
                Value(entry[12]), Value(entry[13])));
        }
        return events;
    }

    private static string Filter(string property, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        if (!SafeFilter.IsMatch(value))
            throw new PostHogApiException("POSTHOG_INVALID_CONFIGURATION", "Filtro UTM no válido.");
        return $" AND lower(properties.{property}) = '{value.ToLowerInvariant()}'";
    }

    private async Task<List<PostHogQueryRow>> RequestRowsAsync(
        string region, int projectId, string personalApiKey, string sql, string name, CancellationToken cancellationToken)
    {
        var results = await RequestResultRowsAsync(region, projectId, personalApiKey, sql, name, cancellationToken);
        var rows = new List<PostHogQueryRow>(results.Count);
        foreach (var entry in results)
        {
            if (entry.ValueKind != JsonValueKind.Array || entry.GetArrayLength() != 9)
                throw new PostHogApiException("POSTHOG_INVALID_RESPONSE", "PostHog devolvió una fila con formato inesperado.");
            if (!DateOnly.TryParseExact(Value(entry[0]), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                throw new PostHogApiException("POSTHOG_INVALID_RESPONSE", "PostHog devolvió una fecha inválida.");
            rows.Add(new PostHogQueryRow(date, Value(entry[1]), Value(entry[2]), Value(entry[3]),
                Value(entry[4]), Value(entry[5]), Value(entry[6]), Number(entry[7]), Number(entry[8])));
        }
        return rows;
    }

    private async Task<List<JsonElement>> RequestResultRowsAsync(
        string region, int projectId, string personalApiKey, string sql, string name, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://{region}.posthog.com/api/projects/{projectId}/query/");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", personalApiKey);
        request.Content = JsonContent.Create(new { query = new { kind = "HogQLQuery", query = sql }, name });
        HttpResponseMessage response;
        try { response = await _httpClient.SendAsync(request, cancellationToken); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new PostHogApiException("POSTHOG_TIMEOUT", "PostHog no respondió a tiempo."); }
        catch (HttpRequestException)
        { throw new PostHogApiException("POSTHOG_UNAVAILABLE", "No se pudo contactar a PostHog."); }
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var code = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden => "POSTHOG_AUTH_FAILED",
                    System.Net.HttpStatusCode.TooManyRequests => "POSTHOG_RATE_LIMITED",
                    _ => "POSTHOG_API_ERROR",
                };
                throw new PostHogApiException(code, $"PostHog respondió HTTP {(int)response.StatusCode}. Comprueba proyecto, permiso query:read y límites.");
            }

            try
            {
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    throw new PostHogApiException("POSTHOG_INVALID_RESPONSE", "PostHog devolvió una respuesta inesperada.");
                if (root.TryGetProperty("query_status", out var status) && status.ValueKind == JsonValueKind.Object &&
                    status.TryGetProperty("complete", out var complete) && complete.ValueKind == JsonValueKind.False)
                    throw new PostHogApiException("POSTHOG_QUERY_PENDING", "PostHog aún está procesando la consulta; vuelve a sincronizar en unos minutos.");
                if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                    throw new PostHogApiException("POSTHOG_INVALID_RESPONSE", "PostHog no devolvió filas de consulta válidas.");
                return results.EnumerateArray().Select(value => value.Clone()).ToList();
            }
            catch (JsonException)
            { throw new PostHogApiException("POSTHOG_INVALID_RESPONSE", "PostHog devolvió JSON inválido."); }
        }
    }

    private static string Value(JsonElement value) => value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
        ? string.Empty : value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();

    private static int Number(JsonElement value) => value.TryGetInt32(out var number) && number >= 0
        ? number : throw new PostHogApiException("POSTHOG_INVALID_RESPONSE", "PostHog devolvió un conteo inválido.");

    public void Dispose()
    {
        if (_ownsClient) _httpClient.Dispose();
    }
}
