using Dashboard.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Data;

public sealed class DashboardDbContext(DbContextOptions<DashboardDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<DashboardPreference> Preferences => Set<DashboardPreference>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<SocialPost> SocialPosts => Set<SocialPost>();
    public DbSet<MarketingEvent> MarketingEvents => Set<MarketingEvent>();
    public DbSet<SeoOpportunity> SeoOpportunities => Set<SeoOpportunity>();
    public DbSet<ContentPiece> ContentPieces => Set<ContentPiece>();
    public DbSet<EditorialHistory> EditorialHistory => Set<EditorialHistory>();
    public DbSet<ApiRatePlan> ApiRatePlans => Set<ApiRatePlan>();
    public DbSet<ProjectBudget> ProjectBudgets => Set<ProjectBudget>();
    public DbSet<ExecutionRecord> Executions => Set<ExecutionRecord>();
    public DbSet<ExecutionAudit> ExecutionAudit => Set<ExecutionAudit>();
    public DbSet<NotificationPolicy> NotificationPolicies => Set<NotificationPolicy>();
    public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();
    public DbSet<AutomationSchedule> AutomationSchedules => Set<AutomationSchedule>();
    public DbSet<AutomationRun> AutomationRuns => Set<AutomationRun>();
    public DbSet<IntegrationConnection> IntegrationConnections => Set<IntegrationConnection>();
    public DbSet<OAuthStateRecord> OAuthStates => Set<OAuthStateRecord>();
    public DbSet<SearchConsoleMetric> SearchConsoleMetrics => Set<SearchConsoleMetric>();
    public DbSet<AiAutomationSettings> AiAutomationSettings => Set<AiAutomationSettings>();
    public DbSet<XAssistantSettings> XAssistantSettings => Set<XAssistantSettings>();
    public DbSet<XSourcePost> XSourcePosts => Set<XSourcePost>();
    public DbSet<XReplyProposal> XReplyProposals => Set<XReplyProposal>();
    public DbSet<PostHogConnection> PostHogConnections => Set<PostHogConnection>();
    public DbSet<PostHogMetric> PostHogMetrics => Set<PostHogMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var project = modelBuilder.Entity<Project>();
        project.HasKey(item => item.Id);
        project.HasIndex(item => item.Slug).IsUnique();
        project.Property(item => item.Name).HasMaxLength(120);
        project.Property(item => item.Slug).HasMaxLength(120);
        project.Property(item => item.Domain).HasMaxLength(200);
        project.Property(item => item.Type).HasMaxLength(40);
        project.Property(item => item.Status).HasMaxLength(30);
        project.Property(item => item.TimeZone).HasMaxLength(80);
        project.Property(item => item.Environment).HasMaxLength(30);
        project.Property(item => item.Description).HasMaxLength(600);
        project.HasMany(item => item.Goals)
            .WithOne(item => item.Project)
            .HasForeignKey(item => item.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        var goal = modelBuilder.Entity<Goal>();
        goal.HasKey(item => item.Id);
        goal.HasIndex(item => new { item.ProjectId, item.Status });
        goal.Property(item => item.Title).HasMaxLength(160);
        goal.Property(item => item.Description).HasMaxLength(600);
        goal.Property(item => item.Metric).HasMaxLength(100);
        goal.Property(item => item.Status).HasMaxLength(30);

        var preference = modelBuilder.Entity<DashboardPreference>();
        preference.HasKey(item => item.Id);
        preference.Property(item => item.TimeZone).HasMaxLength(80);
        preference.Property(item => item.Currency).HasMaxLength(3);

        var campaign = modelBuilder.Entity<Campaign>();
        campaign.HasKey(item => item.Id);
        campaign.HasIndex(item => new { item.ProjectId, item.Status });
        campaign.Property(item => item.Name).HasMaxLength(140);
        campaign.Property(item => item.Objective).HasMaxLength(500);
        campaign.Property(item => item.Status).HasMaxLength(30);
        campaign.Property(item => item.UtmCampaign).HasMaxLength(120);
        campaign.HasOne(item => item.Project).WithMany(item => item.Campaigns)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var socialPost = modelBuilder.Entity<SocialPost>();
        socialPost.HasKey(item => item.Id);
        socialPost.HasIndex(item => new { item.ProjectId, item.Status });
        socialPost.Property(item => item.Platform).HasMaxLength(40);
        socialPost.Property(item => item.Topic).HasMaxLength(220);
        socialPost.Property(item => item.Format).HasMaxLength(50);
        socialPost.Property(item => item.Status).HasMaxLength(30);
        socialPost.Property(item => item.ExternalUrl).HasMaxLength(1000);
        socialPost.Property(item => item.DataSource).HasMaxLength(30);
        socialPost.HasOne(item => item.Project).WithMany(item => item.SocialPosts)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
        socialPost.HasOne(item => item.Campaign).WithMany(item => item.Posts)
            .HasForeignKey(item => item.CampaignId).OnDelete(DeleteBehavior.SetNull);

        var marketingEvent = modelBuilder.Entity<MarketingEvent>();
        marketingEvent.HasKey(item => item.Id);
        marketingEvent.HasIndex(item => new { item.ProjectId, item.OccurredAt });
        marketingEvent.HasIndex(item => new { item.ProjectId, item.Stage });
        marketingEvent.Property(item => item.Stage).HasMaxLength(30);
        marketingEvent.Property(item => item.Source).HasMaxLength(80);
        marketingEvent.Property(item => item.Medium).HasMaxLength(80);
        marketingEvent.Property(item => item.LandingPath).HasMaxLength(500);
        marketingEvent.Property(item => item.DataSource).HasMaxLength(30);
        marketingEvent.HasOne(item => item.Project).WithMany(item => item.MarketingEvents)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
        marketingEvent.HasOne(item => item.Campaign).WithMany(item => item.Events)
            .HasForeignKey(item => item.CampaignId).OnDelete(DeleteBehavior.SetNull);
        marketingEvent.HasOne(item => item.SocialPost).WithMany(item => item.Events)
            .HasForeignKey(item => item.SocialPostId).OnDelete(DeleteBehavior.SetNull);

        var opportunity = modelBuilder.Entity<SeoOpportunity>();
        opportunity.HasKey(item => item.Id);
        opportunity.HasIndex(item => new { item.ProjectId, item.Status });
        opportunity.Property(item => item.Query).HasMaxLength(220);
        opportunity.Property(item => item.TargetPage).HasMaxLength(500);
        opportunity.Property(item => item.Evidence).HasMaxLength(1000);
        opportunity.Property(item => item.Hypothesis).HasMaxLength(1000);
        opportunity.Property(item => item.Status).HasMaxLength(30);
        opportunity.Property(item => item.DataSource).HasMaxLength(30);
        opportunity.HasOne(item => item.Project).WithMany(item => item.SeoOpportunities)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var contentPiece = modelBuilder.Entity<ContentPiece>();
        contentPiece.HasKey(item => item.Id);
        contentPiece.HasIndex(item => new { item.ProjectId, item.Status });
        contentPiece.HasIndex(item => new { item.ProjectId, item.ScheduledFor });
        contentPiece.Property(item => item.Title).HasMaxLength(220);
        contentPiece.Property(item => item.Slug).HasMaxLength(220);
        contentPiece.Property(item => item.ContentType).HasMaxLength(30);
        contentPiece.Property(item => item.PrimaryKeyword).HasMaxLength(220);
        contentPiece.Property(item => item.SearchIntent).HasMaxLength(80);
        contentPiece.Property(item => item.Hypothesis).HasMaxLength(1200);
        contentPiece.Property(item => item.BaselineSummary).HasMaxLength(1200);
        contentPiece.Property(item => item.Objective).HasMaxLength(800);
        contentPiece.Property(item => item.Owner).HasMaxLength(120);
        contentPiece.Property(item => item.Brief).HasMaxLength(10000);
        contentPiece.Property(item => item.DraftMarkdown).HasMaxLength(60000);
        contentPiece.Property(item => item.MetaTitle).HasMaxLength(180);
        contentPiece.Property(item => item.MetaDescription).HasMaxLength(320);
        contentPiece.Property(item => item.Status).HasMaxLength(30);
        contentPiece.Property(item => item.ResultNotes).HasMaxLength(1500);
        contentPiece.Property(item => item.SimulatedWordPressUrl).HasMaxLength(1000);
        contentPiece.Property(item => item.WordPressStatus).HasMaxLength(30);
        contentPiece.HasIndex(item => new { item.ProjectId, item.WordPressPostId }).IsUnique();
        contentPiece.HasOne(item => item.Project).WithMany(item => item.ContentPieces)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
        contentPiece.HasOne(item => item.SeoOpportunity).WithMany(item => item.ContentPieces)
            .HasForeignKey(item => item.SeoOpportunityId).OnDelete(DeleteBehavior.SetNull);

        var history = modelBuilder.Entity<EditorialHistory>();
        history.HasKey(item => item.Id);
        history.HasIndex(item => new { item.ProjectId, item.ContentPieceId, item.ChangedAt });
        history.Property(item => item.FromStatus).HasMaxLength(30);
        history.Property(item => item.ToStatus).HasMaxLength(30);
        history.Property(item => item.Note).HasMaxLength(1000);
        history.Property(item => item.Actor).HasMaxLength(120);
        history.HasOne(item => item.Project).WithMany(item => item.EditorialHistory)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
        history.HasOne(item => item.ContentPiece).WithMany(item => item.History)
            .HasForeignKey(item => item.ContentPieceId).OnDelete(DeleteBehavior.Cascade);

        var rate = modelBuilder.Entity<ApiRatePlan>();
        rate.HasKey(item => item.Id);
        rate.HasIndex(item => new { item.ProjectId, item.Provider, item.Model, item.EffectiveFrom });
        rate.Property(item => item.Provider).HasMaxLength(80);
        rate.Property(item => item.Model).HasMaxLength(120);
        rate.Property(item => item.InputUsdPerMillion).HasPrecision(18, 8);
        rate.Property(item => item.OutputUsdPerMillion).HasPrecision(18, 8);
        rate.HasOne(item => item.Project).WithMany(item => item.ApiRatePlans)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var budget = modelBuilder.Entity<ProjectBudget>();
        budget.HasKey(item => item.Id);
        budget.HasIndex(item => item.ProjectId).IsUnique();
        budget.Property(item => item.DailyLimitUsd).HasPrecision(18, 6);
        budget.Property(item => item.MonthlyLimitUsd).HasPrecision(18, 6);
        budget.Property(item => item.ExchangeRateGtqPerUsd).HasPrecision(18, 6);
        budget.HasOne(item => item.Project).WithOne(item => item.Budget)
            .HasForeignKey<ProjectBudget>(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var execution = modelBuilder.Entity<ExecutionRecord>();
        execution.HasKey(item => item.Id);
        execution.HasIndex(item => new { item.ProjectId, item.IdempotencyKey }).IsUnique();
        execution.HasIndex(item => new { item.ProjectId, item.CreatedAt });
        execution.HasIndex(item => new { item.ProjectId, item.Status });
        execution.Property(item => item.IdempotencyKey).HasMaxLength(160);
        execution.Property(item => item.Provider).HasMaxLength(80);
        execution.Property(item => item.Model).HasMaxLength(120);
        execution.Property(item => item.Flow).HasMaxLength(120);
        execution.Property(item => item.Status).HasMaxLength(30);
        execution.Property(item => item.ApprovedBy).HasMaxLength(120);
        execution.Property(item => item.EstimatedCostUsd).HasPrecision(18, 8);
        execution.Property(item => item.EstimatedCostGtq).HasPrecision(18, 6);
        execution.Property(item => item.ErrorCode).HasMaxLength(80);
        execution.Property(item => item.ErrorMessage).HasMaxLength(1000);
        execution.HasOne(item => item.Project).WithMany(item => item.Executions)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
        execution.HasOne(item => item.ApiRatePlan).WithMany(item => item.Executions)
            .HasForeignKey(item => item.ApiRatePlanId).OnDelete(DeleteBehavior.Restrict);
        execution.HasOne(item => item.ParentExecution).WithMany(item => item.Retries)
            .HasForeignKey(item => item.ParentExecutionId).OnDelete(DeleteBehavior.Restrict);

        var executionAudit = modelBuilder.Entity<ExecutionAudit>();
        executionAudit.HasKey(item => item.Id);
        executionAudit.HasIndex(item => new { item.ProjectId, item.ExecutionRecordId, item.OccurredAt });
        executionAudit.Property(item => item.EventType).HasMaxLength(60);
        executionAudit.Property(item => item.FromStatus).HasMaxLength(30);
        executionAudit.Property(item => item.ToStatus).HasMaxLength(30);
        executionAudit.Property(item => item.Note).HasMaxLength(1000);
        executionAudit.Property(item => item.Actor).HasMaxLength(120);
        executionAudit.HasOne(item => item.Project).WithMany(item => item.ExecutionAudit)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
        executionAudit.HasOne(item => item.ExecutionRecord).WithMany(item => item.Audit)
            .HasForeignKey(item => item.ExecutionRecordId).OnDelete(DeleteBehavior.Cascade);

        var notificationPolicy = modelBuilder.Entity<NotificationPolicy>();
        notificationPolicy.HasKey(item => item.Id);
        notificationPolicy.HasIndex(item => item.ProjectId).IsUnique();
        notificationPolicy.Property(item => item.DeliveryMode).HasMaxLength(30);
        notificationPolicy.Property(item => item.MinimumSeverity).HasMaxLength(20);
        notificationPolicy.HasOne(item => item.Project).WithOne(item => item.NotificationPolicy)
            .HasForeignKey<NotificationPolicy>(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var notification = modelBuilder.Entity<NotificationRecord>();
        notification.HasKey(item => item.Id);
        notification.HasIndex(item => new { item.ProjectId, item.CreatedAt });
        notification.HasIndex(item => new { item.ProjectId, item.Status });
        notification.HasIndex(item => new { item.ProjectId, item.DeduplicationKey, item.CreatedAt });
        notification.Property(item => item.DeduplicationKey).HasMaxLength(160);
        notification.Property(item => item.Category).HasMaxLength(40);
        notification.Property(item => item.Severity).HasMaxLength(20);
        notification.Property(item => item.Title).HasMaxLength(180);
        notification.Property(item => item.Message).HasMaxLength(1200);
        notification.Property(item => item.Flow).HasMaxLength(100);
        notification.Property(item => item.Provider).HasMaxLength(80);
        notification.Property(item => item.Model).HasMaxLength(120);
        notification.Property(item => item.EstimatedCostUsd).HasPrecision(18, 8);
        notification.Property(item => item.Status).HasMaxLength(30);
        notification.Property(item => item.ErrorMessage).HasMaxLength(1000);
        notification.HasOne(item => item.Project).WithMany(item => item.Notifications)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
        notification.HasOne(item => item.NotificationPolicy).WithMany()
            .HasForeignKey(item => item.NotificationPolicyId).OnDelete(DeleteBehavior.Restrict);

        var automationSchedule = modelBuilder.Entity<AutomationSchedule>();
        automationSchedule.HasKey(item => item.Id);
        automationSchedule.HasIndex(item => new { item.ProjectId, item.Workflow }).IsUnique();
        automationSchedule.HasIndex(item => new { item.IsEnabled, item.NextRunAt });
        automationSchedule.Property(item => item.Workflow).HasMaxLength(80);
        automationSchedule.Property(item => item.DisplayName).HasMaxLength(160);
        automationSchedule.Property(item => item.Frequency).HasMaxLength(20);
        automationSchedule.Property(item => item.LocalTime).HasMaxLength(5);
        automationSchedule.HasOne(item => item.Project).WithMany(item => item.AutomationSchedules)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var automationRun = modelBuilder.Entity<AutomationRun>();
        automationRun.HasKey(item => item.Id);
        automationRun.HasIndex(item => new { item.ProjectId, item.CreatedAt });
        automationRun.HasIndex(item => new { item.AutomationScheduleId, item.Status });
        automationRun.Property(item => item.Trigger).HasMaxLength(20);
        automationRun.Property(item => item.Status).HasMaxLength(30);
        automationRun.Property(item => item.ErrorCode).HasMaxLength(80);
        automationRun.Property(item => item.ErrorMessage).HasMaxLength(1000);
        automationRun.HasOne(item => item.Project).WithMany(item => item.AutomationRuns)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
        automationRun.HasOne(item => item.Schedule).WithMany(item => item.Runs)
            .HasForeignKey(item => item.AutomationScheduleId).OnDelete(DeleteBehavior.Cascade);

        var integration = modelBuilder.Entity<IntegrationConnection>();
        integration.HasKey(item => item.Id);
        integration.HasIndex(item => new { item.ProjectId, item.Provider }).IsUnique();
        integration.Property(item => item.Provider).HasMaxLength(80);
        integration.Property(item => item.Status).HasMaxLength(30);
        integration.Property(item => item.ExternalAccount).HasMaxLength(160);
        integration.Property(item => item.ResourceId).HasMaxLength(500);
        integration.Property(item => item.EncryptedRefreshToken).HasMaxLength(4000);
        integration.Property(item => item.GrantedScopes).HasMaxLength(1000);
        integration.Property(item => item.LastError).HasMaxLength(1000);
        integration.HasOne(item => item.Project).WithMany(item => item.IntegrationConnections)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var oauthState = modelBuilder.Entity<OAuthStateRecord>();
        oauthState.HasKey(item => item.Id);
        oauthState.HasIndex(item => item.StateHash).IsUnique();
        oauthState.Property(item => item.Provider).HasMaxLength(80);
        oauthState.Property(item => item.StateHash).HasMaxLength(64);
        oauthState.HasOne(item => item.Project).WithMany(item => item.OAuthStates)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var searchMetric = modelBuilder.Entity<SearchConsoleMetric>();
        searchMetric.HasKey(item => item.Id);
        searchMetric.HasIndex(item => new { item.ProjectId, item.Date });
        searchMetric.HasIndex(item => new { item.IntegrationConnectionId, item.Date });
        searchMetric.Property(item => item.Query).HasMaxLength(1000);
        searchMetric.Property(item => item.Page).HasMaxLength(2000);
        searchMetric.HasOne(item => item.Project).WithMany(item => item.SearchConsoleMetrics)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
        searchMetric.HasOne(item => item.Connection).WithMany(item => item.SearchConsoleMetrics)
            .HasForeignKey(item => item.IntegrationConnectionId).OnDelete(DeleteBehavior.Cascade);

        var aiSettings = modelBuilder.Entity<AiAutomationSettings>();
        aiSettings.HasKey(item => item.Id);
        aiSettings.HasIndex(item => item.ProjectId).IsUnique();
        aiSettings.Property(item => item.Model).HasMaxLength(120);
        aiSettings.Property(item => item.Owner).HasMaxLength(120);
        aiSettings.Property(item => item.MaximumXProposalsPerDay).HasDefaultValue(1);
        aiSettings.Property(item => item.MaximumTotalGeminiRunsPerDay).HasDefaultValue(2);
        aiSettings.HasOne(item => item.Project).WithOne(item => item.AiAutomationSettings)
            .HasForeignKey<AiAutomationSettings>(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var xSettings = modelBuilder.Entity<XAssistantSettings>();
        xSettings.HasKey(item => item.Id);
        xSettings.HasIndex(item => item.ProjectId).IsUnique();
        xSettings.Property(item => item.SearchQuery).HasMaxLength(512);
        xSettings.Property(item => item.Language).HasMaxLength(10);
        xSettings.Property(item => item.ReadCostUsdPerPost).HasPrecision(18, 8);
        xSettings.Property(item => item.ToneInstructions).HasMaxLength(2000);
        xSettings.Property(item => item.LandingPath).HasMaxLength(500);
        xSettings.Property(item => item.UtmCampaign).HasMaxLength(120);
        xSettings.Property(item => item.LastError).HasMaxLength(1000);
        xSettings.HasOne(item => item.Project).WithOne(item => item.XAssistantSettings)
            .HasForeignKey<XAssistantSettings>(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var xSource = modelBuilder.Entity<XSourcePost>();
        xSource.HasKey(item => item.Id);
        xSource.HasIndex(item => new { item.ProjectId, item.ExternalPostId }).IsUnique();
        xSource.HasIndex(item => new { item.ProjectId, item.Status, item.PostedAt });
        xSource.Property(item => item.ExternalPostId).HasMaxLength(80);
        xSource.Property(item => item.Url).HasMaxLength(1000);
        xSource.Property(item => item.AuthorUsername).HasMaxLength(100);
        xSource.Property(item => item.Text).HasMaxLength(10000);
        xSource.Property(item => item.Language).HasMaxLength(10);
        xSource.Property(item => item.Status).HasMaxLength(30);
        xSource.Property(item => item.DataSource).HasMaxLength(30);
        xSource.HasOne(item => item.Project).WithMany(item => item.XSourcePosts)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var xProposal = modelBuilder.Entity<XReplyProposal>();
        xProposal.HasKey(item => item.Id);
        xProposal.HasIndex(item => new { item.ProjectId, item.Status, item.CreatedAt });
        xProposal.HasIndex(item => item.XSourcePostId).IsUnique();
        xProposal.Property(item => item.RecommendedReply).HasMaxLength(500);
        xProposal.Property(item => item.AlternativeOne).HasMaxLength(500);
        xProposal.Property(item => item.AlternativeTwo).HasMaxLength(500);
        xProposal.Property(item => item.SelectedReply).HasMaxLength(500);
        xProposal.Property(item => item.Rationale).HasMaxLength(2000);
        xProposal.Property(item => item.RiskNotes).HasMaxLength(1000);
        xProposal.Property(item => item.Status).HasMaxLength(30);
        xProposal.Property(item => item.PublishedReplyUrl).HasMaxLength(1000);
        xProposal.Property(item => item.TrackingUrl).HasMaxLength(2000);
        xProposal.HasOne(item => item.Project).WithMany(item => item.XReplyProposals)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
        xProposal.HasOne(item => item.SourcePost).WithMany(item => item.Proposals)
            .HasForeignKey(item => item.XSourcePostId).OnDelete(DeleteBehavior.Cascade);

        var postHogConnection = modelBuilder.Entity<PostHogConnection>();
        postHogConnection.HasKey(item => item.Id);
        postHogConnection.HasIndex(item => item.ProjectId).IsUnique();
        postHogConnection.Property(item => item.Region).HasMaxLength(2);
        postHogConnection.Property(item => item.PublicToken).HasMaxLength(160);
        postHogConnection.Property(item => item.ApiKeyEnvironmentVariable).HasMaxLength(120);
        postHogConnection.Property(item => item.LastError).HasMaxLength(1000);
        postHogConnection.HasOne(item => item.Project).WithOne(item => item.PostHogConnection)
            .HasForeignKey<PostHogConnection>(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var postHogMetric = modelBuilder.Entity<PostHogMetric>();
        postHogMetric.HasKey(item => item.Id);
        postHogMetric.HasIndex(item => new { item.ProjectId, item.Date });
        postHogMetric.HasIndex(item => new { item.ProjectId, item.Date, item.Event, item.Source,
            item.Medium, item.Campaign, item.Content, item.Path }).IsUnique();
        postHogMetric.Property(item => item.Event).HasMaxLength(40);
        postHogMetric.Property(item => item.Source).HasMaxLength(100);
        postHogMetric.Property(item => item.Medium).HasMaxLength(100);
        postHogMetric.Property(item => item.Campaign).HasMaxLength(140);
        postHogMetric.Property(item => item.Content).HasMaxLength(160);
        postHogMetric.Property(item => item.Path).HasMaxLength(300);
        postHogMetric.HasOne(item => item.Project).WithMany(item => item.PostHogMetrics)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
        postHogMetric.HasOne(item => item.Connection).WithMany(item => item.Metrics)
            .HasForeignKey(item => item.PostHogConnectionId).OnDelete(DeleteBehavior.Cascade);
    }
}
