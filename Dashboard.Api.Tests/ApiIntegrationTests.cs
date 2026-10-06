using System.Net;
using System.Net.Http.Json;
using Dashboard.Api.Contracts;
using Dashboard.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        Environment.SetEnvironmentVariable("GOOGLE_SEARCH_CONSOLE_CLIENT_ID", "test-client-id");
        Environment.SetEnvironmentVariable("GOOGLE_SEARCH_CONSOLE_CLIENT_SECRET", "test-client-secret");
        Environment.SetEnvironmentVariable("GOOGLE_SEARCH_CONSOLE_REDIRECT_URI", "http://localhost/api/integrations/search-console/callback");
        Environment.SetEnvironmentVariable("DASHBOARD_WEB_BASE_URL", "http://localhost:5173");
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", "test-gemini-key");
        Environment.SetEnvironmentVariable("X_BEARER_TOKEN", "test-x-token");
        Environment.SetEnvironmentVariable("WORDPRESS_BASE_URL", "https://fyrstudios.com/");
        Environment.SetEnvironmentVariable("WORDPRESS_USERNAME", "dashboard-bot");
        Environment.SetEnvironmentVariable("WORDPRESS_APPLICATION_PASSWORD", "test-wordpress-password");
        Environment.SetEnvironmentVariable("POSTHOG_PERSONAL_API_KEY", "test-posthog-read-key");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.AddLogging(logging => logging.ClearProviders());
                services.RemoveAll<ISearchConsoleApiClient>();
                services.AddSingleton<ISearchConsoleApiClient, FakeSearchConsoleApiClient>();
                services.RemoveAll<IGeminiApiClient>();
                services.AddSingleton<IGeminiApiClient, FakeGeminiApiClient>();
                services.RemoveAll<IWordPressApiClient>();
                services.AddSingleton<IWordPressApiClient, FakeWordPressApiClient>();
                services.RemoveAll<IXApiClient>();
                services.AddSingleton<IXApiClient, FakeXApiClient>();
                services.RemoveAll<IPostHogApiClient>();
                services.AddSingleton<IPostHogApiClient, FakePostHogApiClient>();
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

        var fakeWordPress = Assert.IsType<FakeWordPressApiClient>(_factory.Services.GetRequiredService<IWordPressApiClient>());
        fakeWordPress.FailNextCreate = true;
        var failedWordPressDraft = await client.PostAsync($"/api/projects/{first.Id}/integrations/wordpress/content/{content.Id}/draft", null);
        Assert.Equal(HttpStatusCode.Conflict, failedWordPressDraft.StatusCode);
        var preserved = (await client.GetFromJsonAsync<List<ContentPieceResponse>>($"/api/projects/{first.Id}/seo/content"))!.Single(item => item.Id == content.Id);
        Assert.Equal("scheduled", preserved.Status);
        Assert.Null(preserved.WordPressPostId);

        var wordPressDraftResponse = await client.PostAsync($"/api/projects/{first.Id}/integrations/wordpress/content/{content.Id}/draft", null);
        wordPressDraftResponse.EnsureSuccessStatusCode();
        var wordPressDraft = (await wordPressDraftResponse.Content.ReadFromJsonAsync<ContentPieceResponse>())!;
        Assert.Equal("sent_draft", wordPressDraft.Status);
        Assert.Equal("draft", wordPressDraft.WordPressStatus);
        Assert.Contains("fyrstudios.com", wordPressDraft.WordPressEditUrl);
        Assert.Equal(2, fakeWordPress.CreateCalls);

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

        var firstDispatch = new DispatchNotificationRequest("build-123", "operations", "warning", "Pruebas completadas", "El flujo local terminó.", false,
            "seo_draft", "gemini", "flash-test", 8400, 2100, 0.0012m);
        var sentResponse = await client.PostAsJsonAsync($"/api/projects/{firstProject.Id}/notifications/dispatch", firstDispatch);
        sentResponse.EnsureSuccessStatusCode();
        var sent = (await sentResponse.Content.ReadFromJsonAsync<NotificationResponse>())!;
        Assert.Equal("sent", sent.Status);
        Assert.Equal(1, sent.AttemptCount);
        Assert.Equal("gemini", sent.Provider);
        Assert.Equal(8400, sent.InputUnits);

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

    [Fact]
    public async Task Automations_are_configurable_manual_and_project_isolated()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-password"))).EnsureSuccessStatusCode();
        var first = await CreateProjectAsync(client, "Automatización Alfa", "automation-alpha.local");
        var second = await CreateProjectAsync(client, "Automatización Beta", "automation-beta.local");

        var policyResponse = await client.PutAsJsonAsync($"/api/projects/{first.Id}/notifications/policy",
            new NotificationPolicyRequest(true, "info", 30, 0, 0));
        policyResponse.EnsureSuccessStatusCode();

        var schedules = await client.GetFromJsonAsync<List<AutomationScheduleResponse>>($"/api/projects/{first.Id}/automations");
        Assert.NotNull(schedules);
        Assert.Equal(6, schedules.Count);
        Assert.Contains(schedules, item => item.Workflow == "search_console_sync" && !item.IsEnabled);
        Assert.Contains(schedules, item => item.Workflow == "telegram_daily_summary" && item.IsEnabled);
        Assert.Contains(schedules, item => item.Workflow == "x_response_pipeline" && !item.IsEnabled);
        Assert.Contains(schedules, item => item.Workflow == "posthog_sync" && !item.IsEnabled);

        var update = await client.PutAsJsonAsync($"/api/projects/{first.Id}/automations/search_console_sync",
            new AutomationScheduleRequest(true, "daily", null, "06:30", null, 1));
        update.EnsureSuccessStatusCode();
        var configured = (await update.Content.ReadFromJsonAsync<AutomationScheduleResponse>())!;
        Assert.True(configured.IsEnabled);
        Assert.NotNull(configured.NextRunAt);

        var pendingConnectorResponse = await client.PostAsync($"/api/projects/{first.Id}/automations/search_console_sync/run", null);
        pendingConnectorResponse.EnsureSuccessStatusCode();
        var pendingConnector = (await pendingConnectorResponse.Content.ReadFromJsonAsync<AutomationRunResponse>())!;
        Assert.Equal("blocked", pendingConnector.Status);
        Assert.Equal("SEARCH_CONSOLE_NOT_CONNECTED", pendingConnector.ErrorCode);

        var summaryResponse = await client.PostAsync($"/api/projects/{first.Id}/automations/telegram_daily_summary/run", null);
        summaryResponse.EnsureSuccessStatusCode();
        var summaryRun = (await summaryResponse.Content.ReadFromJsonAsync<AutomationRunResponse>())!;
        Assert.Equal("succeeded", summaryRun.Status);

        var notifications = await client.GetFromJsonAsync<List<NotificationResponse>>($"/api/projects/{first.Id}/notifications");
        Assert.Contains(notifications!, item => item.Flow == "telegram_daily_summary" && item.Status == "sent");
        var secondRuns = await client.GetFromJsonAsync<List<AutomationRunResponse>>($"/api/projects/{second.Id}/automations/runs");
        Assert.Empty(secondRuns!);
    }

    [Fact]
    public async Task Search_console_oauth_sync_summary_and_disconnect_are_safe_and_isolated()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-password"))).EnsureSuccessStatusCode();
        var first = await CreateProjectAsync(client, "FyrStudios OAuth", "fyrstudios.com");
        var second = await CreateProjectAsync(client, "Otro proyecto", "otro.local");
        _ = await client.GetFromJsonAsync<List<AutomationScheduleResponse>>($"/api/projects/{first.Id}/automations");

        var initial = await client.GetFromJsonAsync<SearchConsoleStatusResponse>($"/api/projects/{first.Id}/integrations/search-console/status");
        Assert.NotNull(initial);
        Assert.True(initial.ClientConfigured);
        Assert.False(initial.Connected);

        var authorizationResponse = await client.PostAsync($"/api/projects/{first.Id}/integrations/search-console/authorize", null);
        authorizationResponse.EnsureSuccessStatusCode();
        var authorization = (await authorizationResponse.Content.ReadFromJsonAsync<SearchConsoleAuthorizationResponse>())!;
        var authorizationUri = new Uri(authorization.AuthorizationUrl);
        var state = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(authorizationUri.Query)["state"].ToString();
        Assert.Contains(Uri.EscapeDataString(SearchConsoleService.ReadOnlyScope), authorization.AuthorizationUrl);

        var callback = await client.GetAsync($"/api/integrations/search-console/callback?code=test-code&state={Uri.EscapeDataString(state)}");
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);

        var connected = await client.GetFromJsonAsync<SearchConsoleStatusResponse>($"/api/projects/{first.Id}/integrations/search-console/status");
        Assert.True(connected!.Connected);
        Assert.Equal("sc-domain:fyrstudios.com", connected.Property);

        var syncResponse = await client.PostAsync($"/api/projects/{first.Id}/automations/search_console_sync/run", null);
        syncResponse.EnsureSuccessStatusCode();
        var run = (await syncResponse.Content.ReadFromJsonAsync<AutomationRunResponse>())!;
        Assert.Equal("succeeded", run.Status);

        var summary = await client.GetFromJsonAsync<SearchConsoleSummaryResponse>($"/api/projects/{first.Id}/integrations/search-console/summary");
        Assert.Equal(2, summary!.Rows);
        Assert.Equal(13, summary.Clicks);
        Assert.Equal(500, summary.Impressions);
        Assert.Equal(2, summary.TopQueries.Count);

        var geminiSettings = await client.GetFromJsonAsync<GeminiSettingsResponse>($"/api/projects/{first.Id}/integrations/gemini/settings");
        Assert.True(geminiSettings!.ApiKeyConfigured);
        var settingsUpdate = await client.PutAsJsonAsync($"/api/projects/{first.Id}/integrations/gemini/settings",
            new GeminiSettingsRequest(true, "gemini-test-free", 20, 4, 30, 5, 1, 1, 2, 300, 4096, "Carlos"));
        settingsUpdate.EnsureSuccessStatusCode();

        var wordPressSettings = await client.GetFromJsonAsync<WordPressSettingsResponse>($"/api/projects/{first.Id}/integrations/wordpress/settings");
        Assert.True(wordPressSettings!.PasswordConfigured);
        Assert.Equal("https://fyrstudios.com", wordPressSettings.BaseUrl);
        var wordPressTest = await client.PostAsync($"/api/projects/{first.Id}/integrations/wordpress/test", null);
        wordPressTest.EnsureSuccessStatusCode();

        var seoRunResponse = await client.PostAsync($"/api/projects/{first.Id}/automations/seo_content_pipeline/run", null);
        seoRunResponse.EnsureSuccessStatusCode();
        var seoRun = (await seoRunResponse.Content.ReadFromJsonAsync<AutomationRunResponse>())!;
        Assert.Equal("succeeded", seoRun.Status);
        var generatedContent = await client.GetFromJsonAsync<List<ContentPieceResponse>>($"/api/projects/{first.Id}/seo/content");
        var wordPressDraft = Assert.Single(generatedContent!, item => item.PrimaryKeyword == "diseño web guatemala" && item.Status == "sent_draft" && !item.IsDemoData);
        Assert.Equal(781, wordPressDraft.WordPressPostId);
        Assert.Equal("draft", wordPressDraft.WordPressStatus);
        Assert.Contains("post=781", wordPressDraft.WordPressEditUrl);
        var idempotentRetry = await client.PostAsync($"/api/projects/{first.Id}/integrations/wordpress/content/{wordPressDraft.Id}/draft", null);
        idempotentRetry.EnsureSuccessStatusCode();
        Assert.Equal(1, _factory.Services.GetRequiredService<IWordPressApiClient>() is FakeWordPressApiClient fake ? fake.CreateCalls : -1);
        var executions = await client.GetFromJsonAsync<List<ExecutionResponse>>($"/api/projects/{first.Id}/operations/executions");
        Assert.Contains(executions!, item => item.Provider == "gemini" && item.Status == "succeeded" && item.InputUnits == 640 && item.OutputUnits == 980 && item.EstimatedCostUsd == 0);
        Assert.Contains(executions!, item => item.Provider == "wordpress" && item.Model == "rest-api" && item.Status == "succeeded" && item.EstimatedCostUsd == 0);

        var otherSummary = await client.GetFromJsonAsync<SearchConsoleSummaryResponse>($"/api/projects/{second.Id}/integrations/search-console/summary");
        Assert.Equal(0, otherSummary!.Rows);

        var invalidDisconnect = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete,
            $"/api/projects/{first.Id}/integrations/search-console/connection")
        {
            Content = JsonContent.Create(new SearchConsoleDisconnectRequest("DESConectar")),
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidDisconnect.StatusCode);
        Assert.True((await client.GetFromJsonAsync<SearchConsoleStatusResponse>($"/api/projects/{first.Id}/integrations/search-console/status"))!.Connected);

        var disconnect = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete,
            $"/api/projects/{first.Id}/integrations/search-console/connection")
        {
            Content = JsonContent.Create(new SearchConsoleDisconnectRequest("desconectar")),
        });
        Assert.Equal(HttpStatusCode.NoContent, disconnect.StatusCode);
        Assert.False((await client.GetFromJsonAsync<SearchConsoleStatusResponse>($"/api/projects/{first.Id}/integrations/search-console/status"))!.Connected);
    }

    [Fact]
    public async Task X_assistant_generates_three_options_and_requires_manual_publication()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-password"))).EnsureSuccessStatusCode();
        var project = await CreateProjectAsync(client, "Conversación X", "fyrstudios.com");
        _ = await client.GetFromJsonAsync<List<AutomationScheduleResponse>>($"/api/projects/{project.Id}/automations");

        var settings = await client.GetFromJsonAsync<XAssistantSettingsResponse>($"/api/projects/{project.Id}/x-assistant/settings");
        Assert.NotNull(settings);
        Assert.True(settings.BearerTokenConfigured);
        Assert.False(settings.ApiReadEnabled);

        var sourceResponse = await client.PostAsJsonAsync($"/api/projects/{project.Id}/x-assistant/sources", new XSourcePostRequest(
            "https://x.com/creador/status/1234567890", "creador",
            "¿Qué debería definirse antes de comenzar el diseño de un sitio web?", "es", 12, 1, 2, 0, 500, DateTimeOffset.UtcNow));
        sourceResponse.EnsureSuccessStatusCode();

        var runResponse = await client.PostAsync($"/api/projects/{project.Id}/automations/x_response_pipeline/run", null);
        runResponse.EnsureSuccessStatusCode();
        var run = (await runResponse.Content.ReadFromJsonAsync<AutomationRunResponse>())!;
        Assert.Equal("succeeded", run.Status);
        Assert.Equal(0, (_factory.Services.GetRequiredService<IXApiClient>() as FakeXApiClient)!.SearchCalls);

        var proposals = await client.GetFromJsonAsync<List<XReplyProposalResponse>>($"/api/projects/{project.Id}/x-assistant/proposals");
        var proposal = Assert.Single(proposals!);
        Assert.Equal("pending_review", proposal.Status);
        Assert.NotEqual(proposal.RecommendedReply, proposal.AlternativeOne);
        Assert.NotEqual(proposal.AlternativeOne, proposal.AlternativeTwo);
        Assert.Contains("utm_source=x", proposal.TrackingUrl);
        Assert.Contains("Más información:", XAssistantService.BuildReplyText(proposal.RecommendedReply, proposal.TrackingUrl));
        Assert.Contains("in_reply_to=1234567890", XAssistantService.BuildReplyIntent(proposal.SourceUrl, proposal.RecommendedReply, proposal.TrackingUrl));
        var notifications = await client.GetFromJsonAsync<List<NotificationResponse>>($"/api/projects/{project.Id}/notifications");
        var xNotification = Assert.Single(notifications!, item => item.Flow == "x_response_pipeline");
        Assert.Contains("1️⃣ Recomendada", xNotification.Message);
        Assert.Contains("Post original:", xNotification.Message);
        Assert.DoesNotContain("twitter.com/intent/tweet", xNotification.Message);
        Assert.True(xNotification.Message.Length < 1600);

        var approve = await client.PostAsJsonAsync($"/api/projects/{project.Id}/x-assistant/proposals/{proposal.Id}/transition",
            new XProposalTransitionRequest("approved", proposal.AlternativeOne, null));
        approve.EnsureSuccessStatusCode();
        var copy = await client.PostAsJsonAsync($"/api/projects/{project.Id}/x-assistant/proposals/{proposal.Id}/transition",
            new XProposalTransitionRequest("copied", proposal.AlternativeOne, null));
        copy.EnsureSuccessStatusCode();
        var publish = await client.PostAsJsonAsync($"/api/projects/{project.Id}/x-assistant/proposals/{proposal.Id}/transition",
            new XProposalTransitionRequest("published", proposal.AlternativeOne, "https://x.com/fyrstudios/status/9876543210"));
        publish.EnsureSuccessStatusCode();
        var published = (await publish.Content.ReadFromJsonAsync<XReplyProposalResponse>())!;
        Assert.Equal("published", published.Status);
        Assert.Equal("https://x.com/fyrstudios/status/9876543210", published.PublishedReplyUrl);

        var socialPosts = await client.GetFromJsonAsync<List<SocialPostResponse>>($"/api/projects/{project.Id}/marketing/posts");
        Assert.Contains(socialPosts!, item => item.Platform == "x" && item.Status == "published" && item.DataSource == "manual");
        var executions = await client.GetFromJsonAsync<List<ExecutionResponse>>($"/api/projects/{project.Id}/operations/executions");
        Assert.Contains(executions!, item => item.Provider == "gemini" && item.Flow == "x_response_pipeline" && item.Status == "succeeded" && item.InputUnits == 310 && item.OutputUnits == 190);
    }

    [Fact]
    public async Task X_paid_sync_is_explicit_and_generation_reuses_the_local_queue()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-password"))).EnsureSuccessStatusCode();
        var project = await CreateProjectAsync(client, "Cola eficiente X", "fyrstudios.com");
        _ = await client.GetFromJsonAsync<List<AutomationScheduleResponse>>($"/api/projects/{project.Id}/automations");

        var settingsResponse = await client.PutAsJsonAsync($"/api/projects/{project.Id}/x-assistant/settings",
            new XAssistantSettingsRequest(true, true, "diseño web", "es", 10, 0.005m,
                "Responder con utilidad y sin inventar datos.", "/servicios", "x-assistant"));
        settingsResponse.EnsureSuccessStatusCode();

        var fakeX = Assert.IsType<FakeXApiClient>(_factory.Services.GetRequiredService<IXApiClient>());
        var callsBefore = fakeX.SearchCalls;
        var syncResponse = await client.PostAsync($"/api/projects/{project.Id}/x-assistant/sync", null);
        syncResponse.EnsureSuccessStatusCode();
        var sync = (await syncResponse.Content.ReadFromJsonAsync<XSyncResult>())!;
        Assert.True(sync.Succeeded);
        Assert.Equal(1, sync.ReadPosts);
        Assert.Equal(1, sync.ImportedPosts);
        Assert.Equal(0.005m, sync.EstimatedCostUsd);
        Assert.Equal(callsBefore + 1, fakeX.SearchCalls);

        var runResponse = await client.PostAsync($"/api/projects/{project.Id}/automations/x_response_pipeline/run", null);
        runResponse.EnsureSuccessStatusCode();
        var run = (await runResponse.Content.ReadFromJsonAsync<AutomationRunResponse>())!;
        Assert.Equal("succeeded", run.Status);
        Assert.Equal(callsBefore + 1, fakeX.SearchCalls);
        var executions = await client.GetFromJsonAsync<List<ExecutionResponse>>($"/api/projects/{project.Id}/operations/executions");
        Assert.Single(executions!, item => item.Provider == "x" && item.Flow == "x_response_pipeline");
    }

    [Fact]
    public async Task Gemini_can_use_a_selected_manual_seo_opportunity_without_search_console_data()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-password"))).EnsureSuccessStatusCode();
        var project = await CreateProjectAsync(client, "SEO manual", "fyrstudios.com");
        _ = await client.GetFromJsonAsync<List<AutomationScheduleResponse>>($"/api/projects/{project.Id}/automations");
        var settingsUpdate = await client.PutAsJsonAsync($"/api/projects/{project.Id}/integrations/gemini/settings",
            new GeminiSettingsRequest(true, "gemini-test-free", 20, 4, 30, 5, 1, 1, 2, 300, 4096, "Carlos"));
        settingsUpdate.EnsureSuccessStatusCode();

        var opportunityResponse = await client.PostAsJsonAsync($"/api/projects/{project.Id}/seo/opportunities", new SeoOpportunityRequest(
            "cómo elegir una agencia de diseño web", "/servicios", "Tema validado manualmente con preguntas frecuentes de clientes.",
            "Una guía puede aclarar criterios de decisión.", "selected", 0, 0));
        opportunityResponse.EnsureSuccessStatusCode();
        var opportunity = (await opportunityResponse.Content.ReadFromJsonAsync<SeoOpportunityResponse>())!;

        var runResponse = await client.PostAsync($"/api/projects/{project.Id}/automations/seo_content_pipeline/run", null);
        runResponse.EnsureSuccessStatusCode();
        var run = (await runResponse.Content.ReadFromJsonAsync<AutomationRunResponse>())!;
        Assert.True(run.Status == "succeeded", $"{run.ErrorCode}: {run.ErrorMessage}");

        var opportunities = await client.GetFromJsonAsync<List<SeoOpportunityResponse>>($"/api/projects/{project.Id}/seo/opportunities");
        var converted = Assert.Single(opportunities!, item => item.Id == opportunity.Id);
        Assert.Equal("manual", converted.DataSource);
        Assert.Equal("converted", converted.Status);
        var content = await client.GetFromJsonAsync<List<ContentPieceResponse>>($"/api/projects/{project.Id}/seo/content");
        Assert.Contains(content!, item => item.SeoOpportunityId == opportunity.Id && item.Status == "sent_draft" && item.WordPressStatus == "draft");
    }

    [Fact]
    public async Task PostHog_sync_is_aggregated_idempotent_and_isolated_by_project()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-password"))).EnsureSuccessStatusCode();
        var first = await CreateProjectAsync(client, "PostHog Alfa", "posthog-alfa.local");
        var second = await CreateProjectAsync(client, "PostHog Beta", "posthog-beta.local");
        var baseUrl = $"/api/projects/{first.Id}/integrations/posthog";

        var invalid = await client.PutAsJsonAsync($"{baseUrl}/settings", new PostHogSettingsRequest(
            "us", 123, "phc_1234567890", "WORDPRESS_APPLICATION_PASSWORD", 7, 5000, true));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var settings = await client.PutAsJsonAsync($"{baseUrl}/settings", new PostHogSettingsRequest(
            "us", 123, "phc_1234567890", "POSTHOG_PERSONAL_API_KEY", 7, 5000, true));
        settings.EnsureSuccessStatusCode();
        var status = (await settings.Content.ReadFromJsonAsync<PostHogStatusResponse>())!;
        Assert.True(status.KeyAvailable);
        Assert.Equal("us", status.Region);

        var campaignResponse = await client.PostAsJsonAsync($"/api/projects/{first.Id}/marketing/campaigns", new CampaignRequest(
            "Campaña real", "Medir visitas atribuidas", "active", "prueba-7f", null, null));
        campaignResponse.EnsureSuccessStatusCode();
        var campaign = (await campaignResponse.Content.ReadFromJsonAsync<CampaignResponse>())!;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fake = (FakePostHogApiClient)_factory.Services.GetRequiredService<IPostHogApiClient>();
        fake.Rows.AddRange([
            new(today, "$pageview", "x", "organic", "prueba-7f", "reply-test", "/contacto/", 12, 10),
            new(today, "fyr_quote_request", "x", "organic", "prueba-7f", "reply-test", "/contacto/", 2, 2),
            new(today, "fyr_whatsapp_click", "x", "organic", "prueba-7f", "reply-test", "/contacto/", 1, 1),
            new(today, "$unique_sessions", "", "", "", "", "", 10, 0),
            new(today, "$utm_sessions", "x", "organic", "prueba-7f", "reply-test", "", 10, 0),
        ]);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var runResponse = await client.PostAsync($"/api/projects/{first.Id}/automations/posthog_sync/run", null);
            runResponse.EnsureSuccessStatusCode();
            var run = (await runResponse.Content.ReadFromJsonAsync<AutomationRunResponse>())!;
            Assert.Equal("succeeded", run.Status);
        }
        Assert.Equal(2, fake.Calls);

        var summary = await client.GetFromJsonAsync<PostHogSummaryResponse>($"{baseUrl}/summary?days=28");
        Assert.NotNull(summary);
        Assert.Equal(12, summary.Pageviews);
        Assert.Equal(10, summary.Sessions);
        Assert.Equal(2, summary.QuoteRequests);
        Assert.Equal(1, summary.WhatsAppClicks);
        var pageview = Assert.Single(summary.Metrics, value => value.Event == "$pageview");
        Assert.Equal(campaign.Id, pageview.CampaignId);
        Assert.Equal("reply-test", pageview.Content);
        var attribution = Assert.Single(summary.Attribution);
        Assert.Equal(12, attribution.Pageviews);
        Assert.Equal(10, attribution.Visits);
        Assert.Equal(2, attribution.QuoteRequests);
        Assert.Equal(campaign.Id, attribution.CampaignId);

        fake.RecentEvents.Add(new PostHogEventDetailResponse(DateTimeOffset.UtcNow, "$pageview", "/contacto/",
            "", "", "", "", "", "l.instagram.com", "Microsoft Edge", "Guatemala City",
            "Guatemala", "Desktop", "Windows", "10"));
        var recent = await client.GetFromJsonAsync<List<PostHogEventDetailResponse>>($"{baseUrl}/events?days=28");
        var detail = Assert.Single(recent!);
        Assert.Equal("instagram", detail.Source);
        Assert.Equal("referrer", detail.Attribution);
        Assert.Equal("Microsoft Edge", detail.Browser);
        Assert.Equal("Guatemala City", detail.City);
        var invalidFilter = await client.GetAsync($"{baseUrl}/events?source={Uri.EscapeDataString("x' OR 1=1")}");
        Assert.Equal(HttpStatusCode.BadRequest, invalidFilter.StatusCode);

        var other = await client.GetFromJsonAsync<PostHogSummaryResponse>($"/api/projects/{second.Id}/integrations/posthog/summary");
        Assert.NotNull(other);
        Assert.Equal(0, other.Pageviews);
        Assert.Empty(other.Metrics);

        fake.Failure = new PostHogApiException("POSTHOG_RATE_LIMITED", "Límite de consulta alcanzado.");
        var failedResponse = await client.PostAsync($"/api/projects/{first.Id}/automations/posthog_sync/run", null);
        failedResponse.EnsureSuccessStatusCode();
        var failed = (await failedResponse.Content.ReadFromJsonAsync<AutomationRunResponse>())!;
        Assert.Equal("failed", failed.Status);
        Assert.Equal("POSTHOG_RATE_LIMITED", failed.ErrorCode);
        var preserved = await client.GetFromJsonAsync<PostHogSummaryResponse>($"{baseUrl}/summary?days=28");
        Assert.Equal(12, preserved!.Pageviews);
        var notifications = await client.GetFromJsonAsync<List<NotificationResponse>>($"/api/projects/{first.Id}/notifications");
        Assert.Contains(notifications!, value => value.Flow == "posthog_sync" && value.Severity == "critical");
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
        Environment.SetEnvironmentVariable("GOOGLE_SEARCH_CONSOLE_CLIENT_ID", null);
        Environment.SetEnvironmentVariable("GOOGLE_SEARCH_CONSOLE_CLIENT_SECRET", null);
        Environment.SetEnvironmentVariable("GOOGLE_SEARCH_CONSOLE_REDIRECT_URI", null);
        Environment.SetEnvironmentVariable("DASHBOARD_WEB_BASE_URL", null);
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", null);
        Environment.SetEnvironmentVariable("X_BEARER_TOKEN", null);
        Environment.SetEnvironmentVariable("WORDPRESS_BASE_URL", null);
        Environment.SetEnvironmentVariable("WORDPRESS_USERNAME", null);
        Environment.SetEnvironmentVariable("WORDPRESS_APPLICATION_PASSWORD", null);
        Environment.SetEnvironmentVariable("POSTHOG_PERSONAL_API_KEY", null);
        if (File.Exists(_databasePath)) File.Delete(_databasePath);
        if (File.Exists($"{_databasePath}-shm")) File.Delete($"{_databasePath}-shm");
        if (File.Exists($"{_databasePath}-wal")) File.Delete($"{_databasePath}-wal");
    }

    private sealed record PreferencePayload(int DefaultDateRangeDays, bool CompactNotifications);
}

[CollectionDefinition("Api integration", DisableParallelization = true)]
public sealed class ApiIntegrationCollection;
