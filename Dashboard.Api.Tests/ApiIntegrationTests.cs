using System.Net;
using System.Net.Http.Json;
using Dashboard.Api.Contracts;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Dashboard.Api.Tests;

[Collection("Api integration")]
public sealed class ApiIntegrationTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"dashboard-tests-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests()
    {
        Environment.SetEnvironmentVariable("DASHBOARD_ADMIN_PASSWORD", "test-password");
        Environment.SetEnvironmentVariable("ConnectionStrings__Dashboard", $"Data Source={_databasePath};Pooling=False");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.AddLogging(logging => logging.ClearProviders());
            });
        });
    }

    [Fact]
    public async Task Projects_require_an_authenticated_local_session()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_rejects_an_invalid_password()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("incorrect"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Projects_and_goals_stay_isolated_by_project_id()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-password"));
        login.EnsureSuccessStatusCode();

        var first = await CreateProjectAsync(client, "Proyecto Alfa", "alfa.local");
        var second = await CreateProjectAsync(client, "Proyecto Beta", "beta.local");

        var goalResponse = await client.PostAsJsonAsync($"/api/projects/{first.Id}/goals", new GoalRequest(
            "Meta aislada", "Solo pertenece al proyecto Alfa", "contactos", 10, "active", null));
        goalResponse.EnsureSuccessStatusCode();
        var createdGoal = (await goalResponse.Content.ReadFromJsonAsync<GoalResponse>())!;

        var updateResponse = await client.PutAsJsonAsync($"/api/projects/{first.Id}/goals/{createdGoal.Id}", new GoalRequest(
            "Meta actualizada", "Continúa aislada en Alfa", "contactos", 12, "active", null));
        updateResponse.EnsureSuccessStatusCode();

        var firstDetail = await client.GetFromJsonAsync<ProjectResponse>($"/api/projects/{first.Id}");
        var secondDetail = await client.GetFromJsonAsync<ProjectResponse>($"/api/projects/{second.Id}");

        Assert.NotNull(firstDetail);
        Assert.NotNull(secondDetail);
        Assert.Single(firstDetail.Goals);
        Assert.Equal(first.Id, firstDetail.Goals.Single().ProjectId);
        Assert.Equal("Meta actualizada", firstDetail.Goals.Single().Title);
        Assert.Equal(12, firstDetail.Goals.Single().TargetValue);
        Assert.Empty(secondDetail.Goals);

        var deleteResponse = await client.DeleteAsync($"/api/projects/{first.Id}/goals/{createdGoal.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var afterDelete = await client.GetFromJsonAsync<ProjectResponse>($"/api/projects/{first.Id}");
        Assert.NotNull(afterDelete);
        Assert.Empty(afterDelete.Goals);
    }

    [Fact]
    public async Task Preferences_can_be_updated_after_login()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-password"))).EnsureSuccessStatusCode();

        var response = await client.PutAsJsonAsync("/api/settings", new PreferenceRequest(
            "America/Guatemala", "GTQ", 45, false));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<PreferencePayload>();

        Assert.NotNull(payload);
        Assert.Equal(45, payload.DefaultDateRangeDays);
        Assert.False(payload.CompactNotifications);
    }

    private static async Task<ProjectResponse> CreateProjectAsync(HttpClient client, string name, string domain)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new ProjectRequest(
            name, domain, "SaaS", "active", "America/Guatemala", "local", "Proyecto de prueba"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectResponse>())!;
    }

    public void Dispose()
    {
        _factory.Dispose();
        SqliteConnection.ClearAllPools();
        Environment.SetEnvironmentVariable("DASHBOARD_ADMIN_PASSWORD", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__Dashboard", null);
        if (File.Exists(_databasePath)) File.Delete(_databasePath);
        if (File.Exists($"{_databasePath}-shm")) File.Delete($"{_databasePath}-shm");
        if (File.Exists($"{_databasePath}-wal")) File.Delete($"{_databasePath}-wal");
    }

    private sealed record PreferencePayload(int DefaultDateRangeDays, bool CompactNotifications);
}

[CollectionDefinition("Api integration", DisableParallelization = true)]
public sealed class ApiIntegrationCollection;
