using System.Net;
using System.Text;
using System.Text.Json;
using Dashboard.Api.Services;
using Xunit;

namespace Dashboard.Api.Tests;

public sealed class PostHogApiClientTests
{
    [Fact]
    public async Task Session_queries_do_not_shadow_the_event_column()
    {
        var handler = new QueryHandler();
        using var http = new HttpClient(handler);
        using var client = new PostHogApiClient(http);

        var rows = await client.QueryAsync("us", 123, "test-key", new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 7), 100, CancellationToken.None);

        Assert.Equal(3, rows.Count);
        Assert.Contains(rows, value => value.Event == "$unique_sessions" && value.EventCount == 6);
        Assert.Contains(rows, value => value.Event == "$utm_sessions" && value.EventCount == 2);
        Assert.All(handler.Sql.Skip(1), sql =>
        {
            Assert.Contains("AS metric_event", sql);
            Assert.DoesNotContain("AS event,", sql);
        });
    }

    [Fact]
    public async Task Detail_query_selects_only_requested_technical_fields_and_rejects_unsafe_filters()
    {
        var handler = new QueryHandler();
        using var http = new HttpClient(handler);
        using var client = new PostHogApiClient(http);

        var rows = await client.QueryRecentEventsAsync("us", 123, "test-key", new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 7), "x", "prueba-7f", "reply-prueba", 30, CancellationToken.None);

        var detail = Assert.Single(rows);
        Assert.Equal("Microsoft Edge", detail.Browser);
        Assert.Equal("Guatemala City", detail.City);
        Assert.Contains("lower(properties.utm_source) = 'x'", handler.Sql.Single());
        Assert.DoesNotContain("distinct_id", handler.Sql.Single(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("properties.$ip", handler.Sql.Single(), StringComparison.OrdinalIgnoreCase);
        await Assert.ThrowsAsync<PostHogApiException>(() => client.QueryRecentEventsAsync("us", 123, "test-key",
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 7), "x' OR 1=1", null, null, 30, CancellationToken.None));
        Assert.Single(handler.Sql);
    }

    private sealed class QueryHandler : HttpMessageHandler
    {
        public List<string> Sql { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var sql = body.RootElement.GetProperty("query").GetProperty("query").GetString()!;
            Sql.Add(sql);
            var results = sql.Contains("toUnixTimestamp(timestamp)", StringComparison.Ordinal)
                    ? "[[1791246857,\"$pageview\",\"/contacto/\",\"x\",\"organic\",\"prueba-7f\",\"reply-prueba\",\"$direct\",\"Microsoft Edge\",\"Guatemala City\",\"Guatemala\",\"Desktop\",\"Windows\",\"10\"]]"
                    : sql.Contains("'$unique_sessions'", StringComparison.Ordinal)
                        ? "[[\"2026-10-05\",\"$unique_sessions\",\"\",\"\",\"\",\"\",\"\",6,0]]"
                        : sql.Contains("'$utm_sessions'", StringComparison.Ordinal)
                            ? "[[\"2026-10-05\",\"$utm_sessions\",\"x\",\"organic\",\"prueba-7f\",\"reply-prueba\",\"\",2,0]]"
                            : "[[\"2026-10-05\",\"$pageview\",\"x\",\"organic\",\"prueba-7f\",\"reply-prueba\",\"/contacto/\",20,6]]";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"results\":{results}}}", Encoding.UTF8, "application/json"),
            };
        }
    }
}
