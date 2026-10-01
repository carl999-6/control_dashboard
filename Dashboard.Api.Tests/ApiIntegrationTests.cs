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

    [Fact]
    public async Task Marketing_flow_imports_aggregates_and_preserves_project_isolation()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-password"))).EnsureSuccessStatusCode();
        var first = await CreateProjectAsync(client, "Marketing Alfa", "marketing-alfa.local");
        var second = await CreateProjectAsync(client, "Marketing Beta", "marketing-beta.local");

        var campaignResponse = await client.PostAsJsonAsync($"/api/projects/{first.Id}/marketing/campaigns", new CampaignRequest(
            "Lanzamiento", "Medir interés sin identificar personas", "active", "lanzamiento-q4", null, null));
        campaignResponse.EnsureSuccessStatusCode();
        var campaign = (await campaignResponse.Content.ReadFromJsonAsync<CampaignResponse>())!;

        var postResponse = await client.PostAsJsonAsync($"/api/projects/{first.Id}/marketing/posts", new SocialPostRequest(
            campaign.Id, "instagram", "Anuncio de lanzamiento", "carrusel", "published", "https://example.test/post",
            DateTimeOffset.UtcNow, 1000, 120, 45));
        postResponse.EnsureSuccessStatusCode();
        var post = (await postResponse.Content.ReadFromJsonAsync<SocialPostResponse>())!;

        var updateCampaign = await client.PutAsJsonAsync($"/api/projects/{first.Id}/marketing/campaigns/{campaign.Id}", new CampaignRequest(
            "Lanzamiento actualizado", "Medir interés agregado", "paused", "lanzamiento-q4", null, null));
        var updatePost = await client.PutAsJsonAsync($"/api/projects/{first.Id}/marketing/posts/{post.Id}", new SocialPostRequest(
            campaign.Id, "instagram", "Anuncio actualizado", "carrusel", "published", "https://example.test/post",
            post.PublishedAt, 1050, 125, 48));
        updateCampaign.EnsureSuccessStatusCode();
        updatePost.EnsureSuccessStatusCode();

        var crossProjectPost = await client.PostAsJsonAsync($"/api/projects/{second.Id}/marketing/posts", new SocialPostRequest(
            campaign.Id, "x", "No debe asociarse", "post", "draft", string.Empty, null, 0, 0, 0));
        Assert.Equal(HttpStatusCode.BadRequest, crossProjectPost.StatusCode);

        var importResponse = await client.PostAsJsonAsync($"/api/projects/{first.Id}/marketing/events/import", new MarketingImportRequest(
        [
            new MarketingEventImportItem("visit", "instagram", "social", "/landing", 100, null, campaign.Id, post.Id, "import_csv"),
            new MarketingEventImportItem("contact", "instagram", "social", "/contacto", 8, null, campaign.Id, post.Id, "import_csv"),
        ]));
        importResponse.EnsureSuccessStatusCode();

        var firstSummary = await client.GetFromJsonAsync<MarketingSummaryResponse>($"/api/projects/{first.Id}/marketing/summary");
        var secondSummary = await client.GetFromJsonAsync<MarketingSummaryResponse>($"/api/projects/{second.Id}/marketing/summary");
        var campaigns = await client.GetFromJsonAsync<List<CampaignResponse>>($"/api/projects/{first.Id}/marketing/campaigns");
        var posts = await client.GetFromJsonAsync<List<SocialPostResponse>>($"/api/projects/{first.Id}/marketing/posts");
        Assert.NotNull(firstSummary);
        Assert.NotNull(secondSummary);
        Assert.Equal(100, firstSummary.Funnel.Single(item => item.Stage == "visit").Count);
        Assert.Equal(8, firstSummary.Funnel.Single(item => item.Stage == "contact").Count);
        Assert.Equal(1, firstSummary.Campaigns);
        Assert.Equal(1, firstSummary.Posts);
        Assert.Equal(0, secondSummary.Funnel.Single(item => item.Stage == "visit").Count);
        Assert.Equal(0, secondSummary.Campaigns);
        Assert.Equal(0, secondSummary.Posts);
        Assert.NotNull(campaigns);
        Assert.NotNull(posts);
        Assert.Single(campaigns);
        Assert.Single(posts);
        Assert.Equal("Lanzamiento actualizado", campaigns.Single().Name);
        Assert.Equal(1050, posts.Single().Impressions);

        var deletePost = await client.DeleteAsync($"/api/projects/{first.Id}/marketing/posts/{post.Id}");
        var deleteCampaign = await client.DeleteAsync($"/api/projects/{first.Id}/marketing/campaigns/{campaign.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deletePost.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteCampaign.StatusCode);
    }

    [Fact]
    public async Task Seo_editorial_flow_enforces_transitions_history_and_project_isolation()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-password"))).EnsureSuccessStatusCode();
        var first = await CreateProjectAsync(client, "SEO Alfa", "seo-alfa.local");
        var second = await CreateProjectAsync(client, "SEO Beta", "seo-beta.local");

        var opportunityResponse = await client.PostAsJsonAsync($"/api/projects/{first.Id}/seo/opportunities", new SeoOpportunityRequest(
            "consulta objetivo", "/landing", "100 impresiones y CTR bajo", "Una guía puede responder mejor", "detected", 100, 2));
        opportunityResponse.EnsureSuccessStatusCode();
        var opportunity = (await opportunityResponse.Content.ReadFromJsonAsync<SeoOpportunityResponse>())!;

        var crossProjectContent = await client.PostAsJsonAsync($"/api/projects/{second.Id}/seo/content", ContentRequest(opportunity.Id));
        Assert.Equal(HttpStatusCode.BadRequest, crossProjectContent.StatusCode);

        var contentResponse = await client.PostAsJsonAsync($"/api/projects/{first.Id}/seo/content", ContentRequest(opportunity.Id));
        contentResponse.EnsureSuccessStatusCode();
        var content = (await contentResponse.Content.ReadFromJsonAsync<ContentPieceResponse>())!;
        Assert.Equal("brief", content.Status);
        Assert.Single(content.History);

        var invalidTransition = await client.PostAsJsonAsync($"/api/projects/{first.Id}/seo/content/{content.Id}/transition",
            new EditorialTransitionRequest("approved", null, null));
        Assert.Equal(HttpStatusCode.BadRequest, invalidTransition.StatusCode);

        foreach (var target in new[] { "draft", "pending_review", "approved" })
        {
            var response = await client.PostAsJsonAsync($"/api/projects/{first.Id}/seo/content/{content.Id}/transition",
                new EditorialTransitionRequest(target, $"Paso a {target}", null));
            response.EnsureSuccessStatusCode();
        }
        var scheduledFor = DateTimeOffset.UtcNow.AddDays(4);
        var schedule = await client.PostAsJsonAsync($"/api/projects/{first.Id}/seo/content/{content.Id}/transition",
            new EditorialTransitionRequest("scheduled", "Fecha acordada", scheduledFor));
        schedule.EnsureSuccessStatusCode();

        var simulation = await client.PostAsync($"/api/projects/{first.Id}/seo/content/{content.Id}/simulate-wordpress-draft", null);
        simulation.EnsureSuccessStatusCode();
        var simulated = (await simulation.Content.ReadFromJsonAsync<ContentPieceResponse>())!;
        Assert.Equal("sent_draft", simulated.Status);
        Assert.Contains("wordpress.local", simulated.SimulatedWordPressUrl);

        var measurement = await client.PutAsJsonAsync($"/api/projects/{first.Id}/seo/content/{content.Id}/measurement",
            new ContentMeasurementRequest(320, 14, "Mejora observada, sin afirmar causalidad.", null));
        measurement.EnsureSuccessStatusCode();
        var measured = (await measurement.Content.ReadFromJsonAsync<ContentPieceResponse>())!;
        Assert.Equal("measured", measured.Status);
        Assert.Equal(320, measured.ResultImpressions);
        Assert.Equal(7, measured.History.Count);

        var secondContent = await client.GetFromJsonAsync<List<ContentPieceResponse>>($"/api/projects/{second.Id}/seo/content");
        Assert.NotNull(secondContent);
        Assert.Empty(secondContent);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/projects/{first.Id}/seo/content/{content.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/projects/{first.Id}/seo/opportunities/{opportunity.Id}")).StatusCode);
    }

    [Fact]
    public async Task Execution_planning_is_idempotent_under_concurrent_requests()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-password"))).EnsureSuccessStatusCode();
        var project = await CreateProjectAsync(client, "Operaciones Alfa", "operaciones-alfa.local");

        await ConfigureOperationsAsync(client, project.Id);
        var request = new PlanExecutionRequest("same-operation-key", "gemini", "flash-test", "seo_brief", 1_000, 500, true);
        var firstRequest = client.PostAsJsonAsync($"/api/projects/{project.Id}/operations/executions/plan", request);
        var secondRequest = client.PostAsJsonAsync($"/api/projects/{project.Id}/operations/executions/plan", request);
        var responses = await Task.WhenAll(firstRequest, secondRequest);

        foreach (var response in responses) response.EnsureSuccessStatusCode();
        var first = (await responses[0].Content.ReadFromJsonAsync<ExecutionResponse>())!;
        var second = (await responses[1].Content.ReadFromJsonAsync<ExecutionResponse>())!;
        var executions = await client.GetFromJsonAsync<List<ExecutionResponse>>($"/api/projects/{project.Id}/operations/executions");

        Assert.Equal(first.Id, second.Id);
        Assert.Equal("awaiting_approval", first.Status);
        Assert.NotNull(executions);
        Assert.Single(executions);
        Assert.Equal(0.00125m, first.EstimatedCostUsd);
    }

    [Fact]
    public async Task Execution_controls_approve_fail_retry_and_enforce_budget_pause_and_isolation()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-password"))).EnsureSuccessStatusCode();
        var firstProject = await CreateProjectAsync(client, "Control Alfa", "control-alfa.local");
        var secondProject = await CreateProjectAsync(client, "Control Beta", "control-beta.local");
        await ConfigureOperationsAsync(client, firstProject.Id);

        var planResponse = await client.PostAsJsonAsync($"/api/projects/{firstProject.Id}/operations/executions/plan",
            new PlanExecutionRequest("approval-flow", "gemini", "flash-test", "content_draft", 1_000, 500, true));
        planResponse.EnsureSuccessStatusCode();
        var planned = (await planResponse.Content.ReadFromJsonAsync<ExecutionResponse>())!;

        var approveResponse = await client.PostAsJsonAsync($"/api/projects/{firstProject.Id}/operations/executions/{planned.Id}/approve",
            new ApprovalRequest("Revisión humana completada"));
        approveResponse.EnsureSuccessStatusCode();
        var approved = (await approveResponse.Content.ReadFromJsonAsync<ExecutionResponse>())!;
        Assert.Equal("approved", approved.Status);
        Assert.Equal("Administrador local", approved.ApprovedBy);

        var completeResponse = await client.PostAsJsonAsync($"/api/projects/{firstProject.Id}/operations/executions/{planned.Id}/complete",
            new CompleteExecutionRequest(false, "SIMULATED_FAILURE", "Fallo controlado"));
        completeResponse.EnsureSuccessStatusCode();
        var failed = (await completeResponse.Content.ReadFromJsonAsync<ExecutionResponse>())!;
        Assert.Equal("failed", failed.Status);
        Assert.Equal("SIMULATED_FAILURE", failed.ErrorCode);

        var retryResponse = await client.PostAsync($"/api/projects/{firstProject.Id}/operations/executions/{planned.Id}/retry", null);
        retryResponse.EnsureSuccessStatusCode();
        var retry = (await retryResponse.Content.ReadFromJsonAsync<ExecutionResponse>())!;
        Assert.Equal(planned.Id, retry.ParentExecutionId);
        Assert.Equal(2, retry.AttemptNumber);
        Assert.Equal("awaiting_approval", retry.Status);

        var tinyBudgetResponse = await client.PutAsJsonAsync($"/api/projects/{firstProject.Id}/operations/budget",
            new BudgetRequest(0.0001m, 0.0001m, 80, 7.75m, false));
        tinyBudgetResponse.EnsureSuccessStatusCode();
        var blockedResponse = await client.PostAsJsonAsync($"/api/projects/{firstProject.Id}/operations/executions/plan",
            new PlanExecutionRequest("budget-block", "gemini", "flash-test", "seo_analysis", 2_000, 1_000, false));
        blockedResponse.EnsureSuccessStatusCode();
        var blocked = (await blockedResponse.Content.ReadFromJsonAsync<ExecutionResponse>())!;
        Assert.Equal("blocked", blocked.Status);
        Assert.Equal("BUDGET_BLOCKED", blocked.ErrorCode);

        var pausedBudgetResponse = await client.PutAsJsonAsync($"/api/projects/{firstProject.Id}/operations/budget",
            new BudgetRequest(1m, 10m, 80, 7.75m, true));
        pausedBudgetResponse.EnsureSuccessStatusCode();
        var pausedResponse = await client.PostAsJsonAsync($"/api/projects/{firstProject.Id}/operations/executions/plan",
            new PlanExecutionRequest("paused-block", "gemini", "flash-test", "seo_analysis", 100, 50, false));
        pausedResponse.EnsureSuccessStatusCode();
        Assert.Equal("blocked", (await pausedResponse.Content.ReadFromJsonAsync<ExecutionResponse>())!.Status);

        var secondExecutions = await client.GetFromJsonAsync<List<ExecutionResponse>>($"/api/projects/{secondProject.Id}/operations/executions");
        Assert.NotNull(secondExecutions);
        Assert.Empty(secondExecutions);
    }

    [Fact]
    public async Task Notification_simulation_groups_duplicates_records_failures_retries_and_respects_policy()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-password"))).EnsureSuccessStatusCode();
        var firstProject = await CreateProjectAsync(client, "Alertas Alfa", "alertas-alfa.local");
        var secondProject = await CreateProjectAsync(client, "Alertas Beta", "alertas-beta.local");

        var policyResponse = await client.PutAsJsonAsync($"/api/projects/{firstProject.Id}/notifications/policy",
            new NotificationPolicyRequest(true, "info", 60, 0, 0));
        policyResponse.EnsureSuccessStatusCode();
        var policy = (await policyResponse.Content.ReadFromJsonAsync<NotificationPolicyResponse>())!;
        Assert.Equal("simulated", policy.DeliveryMode);

        var firstDispatch = new DispatchNotificationRequest("build-123", "operations", "warning", "Pruebas completadas", "El flujo local terminó.", false);
        var sentResponse = await client.PostAsJsonAsync($"/api/projects/{firstProject.Id}/notifications/dispatch", firstDispatch);
        sentResponse.EnsureSuccessStatusCode();
        var sent = (await sentResponse.Content.ReadFromJsonAsync<NotificationResponse>())!;
        Assert.Equal("sent", sent.Status);
        Assert.Equal(1, sent.AttemptCount);

        var groupedResponse = await client.PostAsJsonAsync($"/api/projects/{firstProject.Id}/notifications/dispatch", firstDispatch);
        groupedResponse.EnsureSuccessStatusCode();
        var grouped = (await groupedResponse.Content.ReadFromJsonAsync<NotificationResponse>())!;
        Assert.Equal(sent.Id, grouped.Id);
        Assert.Equal("grouped", grouped.Status);
        Assert.Equal(2, grouped.GroupCount);

        var failedResponse = await client.PostAsJsonAsync($"/api/projects/{firstProject.Id}/notifications/dispatch",
            new DispatchNotificationRequest("provider-down", "operations", "critical", "Fallo del proveedor", "Fallo simulado para reintentar.", true));
        failedResponse.EnsureSuccessStatusCode();
        var failed = (await failedResponse.Content.ReadFromJsonAsync<NotificationResponse>())!;
        Assert.Equal("failed", failed.Status);
        Assert.Contains("simulado", failed.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        var retryResponse = await client.PostAsync($"/api/projects/{firstProject.Id}/notifications/{failed.Id}/retry", null);
        retryResponse.EnsureSuccessStatusCode();
        var recovered = (await retryResponse.Content.ReadFromJsonAsync<NotificationResponse>())!;
        Assert.Equal("sent", recovered.Status);
        Assert.Equal(2, recovered.AttemptCount);
        Assert.Empty(recovered.ErrorMessage);

        var criticalOnly = await client.PutAsJsonAsync($"/api/projects/{firstProject.Id}/notifications/policy",
            new NotificationPolicyRequest(true, "critical", 60, 0, 0));
        criticalOnly.EnsureSuccessStatusCode();
        var suppressedResponse = await client.PostAsJsonAsync($"/api/projects/{firstProject.Id}/notifications/dispatch",
            new DispatchNotificationRequest("low-priority", "editorial", "info", "Nota informativa", "No debe entregarse por el umbral.", false));
        suppressedResponse.EnsureSuccessStatusCode();
        Assert.Equal("suppressed", (await suppressedResponse.Content.ReadFromJsonAsync<NotificationResponse>())!.Status);

        var secondItems = await client.GetFromJsonAsync<List<NotificationResponse>>($"/api/projects/{secondProject.Id}/notifications");
        Assert.NotNull(secondItems);
        Assert.Empty(secondItems);
    }

    private static async Task<ProjectResponse> CreateProjectAsync(HttpClient client, string name, string domain)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new ProjectRequest(
            name, domain, "SaaS", "active", "America/Guatemala", "local", "Proyecto de prueba"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectResponse>())!;
    }

    private static async Task ConfigureOperationsAsync(HttpClient client, Guid projectId)
    {
        var rate = await client.PostAsJsonAsync($"/api/projects/{projectId}/operations/rates",
            new RatePlanRequest("gemini", "flash-test", 0.5m, 1.5m, null));
        rate.EnsureSuccessStatusCode();
        var budget = await client.PutAsJsonAsync($"/api/projects/{projectId}/operations/budget",
            new BudgetRequest(1m, 10m, 80, 7.75m, false));
        budget.EnsureSuccessStatusCode();
    }

    private static ContentPieceRequest ContentRequest(Guid opportunityId) => new(
        opportunityId, "Guía de prueba", "new", "consulta objetivo", "informational",
        "La guía puede responder la consulta", "100 impresiones y 2 clics", "Mejorar claridad y CTR",
        "Carlos", "Estructura y preguntas clave", "# Borrador\n\nContenido de prueba.",
        "Guía de prueba", "Descripción de prueba", null);

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
