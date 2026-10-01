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
    }
}
