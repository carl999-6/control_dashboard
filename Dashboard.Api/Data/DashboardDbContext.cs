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
    }
}
