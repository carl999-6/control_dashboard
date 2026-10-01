using Dashboard.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Data;

public sealed class DashboardDbContext(DbContextOptions<DashboardDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<DashboardPreference> Preferences => Set<DashboardPreference>();

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
    }
}
