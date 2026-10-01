using Dashboard.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Data;

public static class SeedData
{
    public static readonly Guid FyrStudiosId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid FutureSaasId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    public static readonly Guid DemoClientId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    public static readonly Guid FyrCampaignId = Guid.Parse("11111111-bbbb-4111-8111-111111111111");
    public static readonly Guid FyrInstagramPostId = Guid.Parse("11111111-cccc-4111-8111-111111111111");
    public static readonly Guid FyrXPostId = Guid.Parse("11111111-dddd-4111-8111-111111111111");

    public static async Task InitializeAsync(DashboardDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Projects.AnyAsync(cancellationToken))
        {
            var createdAt = DateTimeOffset.UtcNow;
            db.Projects.AddRange(
                new Project
                {
                    Id = FyrStudiosId,
                    Name = "FyrStudios",
                    Slug = "fyrstudios",
                    Domain = "fyrstudios.com",
                    Type = "WordPress",
                    Status = "active",
                    TimeZone = "America/Guatemala",
                    Environment = "production",
                    Description = "Primer proyecto conectado al centro de control.",
                    IsDemoData = true,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                    Goals =
                    [
                        new Goal
                        {
                            Id = Guid.Parse("11111111-aaaa-4111-8111-111111111111"),
                            Title = "Aumentar solicitudes de cotización",
                            Description = "Medir formularios y contactos atribuidos a campañas.",
                            Metric = "cotizaciones_mensuales",
                            TargetValue = 25,
                            Status = "active",
                            CreatedAt = createdAt,
                            UpdatedAt = createdAt,
                        },
                    ],
                },
                new Project
                {
                    Id = FutureSaasId,
                    Name = "SaaS futuro",
                    Slug = "saas-futuro",
                    Domain = "Sin dominio",
                    Type = "SaaS",
                    Status = "planning",
                    TimeZone = "America/Guatemala",
                    Environment = "planning",
                    Description = "Proyecto simulado para validar la arquitectura multi-proyecto.",
                    IsDemoData = true,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                },
                new Project
                {
                    Id = DemoClientId,
                    Name = "Cliente demo",
                    Slug = "cliente-demo",
                    Domain = "demo.local",
                    Type = "WordPress",
                    Status = "paused",
                    TimeZone = "America/Guatemala",
                    Environment = "demo",
                    Description = "Datos de ejemplo; no corresponde a un cliente real.",
                    IsDemoData = true,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                });
        }

        if (!await db.Preferences.AnyAsync(cancellationToken))
        {
            db.Preferences.Add(new DashboardPreference
            {
                Id = 1,
                TimeZone = "America/Guatemala",
                Currency = "GTQ",
                DefaultDateRangeDays = 30,
                CompactNotifications = true,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }

        if (!await db.Campaigns.AnyAsync(item => item.ProjectId == FyrStudiosId, cancellationToken))
        {
            var now = DateTimeOffset.UtcNow;
            db.Campaigns.Add(new Campaign
            {
                Id = FyrCampaignId,
                ProjectId = FyrStudiosId,
                Name = "Servicios creativos Q4",
                Objective = "Validar interés y solicitudes de cotización desde contenido social.",
                Status = "active",
                UtmCampaign = "servicios-creativos",
                StartDate = DateOnly.FromDateTime(now.AddDays(-28).DateTime),
                EndDate = DateOnly.FromDateTime(now.AddDays(32).DateTime),
                IsDemoData = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
            db.SocialPosts.AddRange(
                new SocialPost
                {
                    Id = FyrInstagramPostId, ProjectId = FyrStudiosId, CampaignId = FyrCampaignId,
                    Platform = "instagram", Topic = "Antes y después de una identidad visual", Format = "carrusel",
                    Status = "published", ExternalUrl = "https://instagram.com/p/demo-fyrstudios", DataSource = "simulated",
                    PublishedAt = now.AddDays(-8), Impressions = 8450, Engagements = 624, Clicks = 318,
                    CreatedAt = now, UpdatedAt = now,
                },
                new SocialPost
                {
                    Id = FyrXPostId, ProjectId = FyrStudiosId, CampaignId = FyrCampaignId,
                    Platform = "x", Topic = "Señales de que tu marca necesita consistencia", Format = "hilo",
                    Status = "published", ExternalUrl = "https://x.com/fyrstudios/status/demo", DataSource = "simulated",
                    PublishedAt = now.AddDays(-4), Impressions = 3920, Engagements = 281, Clicks = 176,
                    CreatedAt = now, UpdatedAt = now,
                });

            var rows = new (string Stage, string Source, string Medium, int Count)[]
            {
                ("visit", "instagram", "social", 1120), ("visit", "x", "social", 574), ("visit", "organic", "search", 490),
                ("interest", "instagram", "social", 282), ("interest", "x", "social", 114), ("interest", "organic", "search", 90),
                ("contact", "instagram", "social", 31), ("contact", "x", "social", 12), ("contact", "organic", "search", 15),
                ("quote", "instagram", "social", 8), ("quote", "x", "social", 4), ("quote", "organic", "search", 5),
            };
            db.MarketingEvents.AddRange(rows.Select((row, index) => new MarketingEvent
            {
                Id = Guid.NewGuid(), ProjectId = FyrStudiosId, CampaignId = FyrCampaignId,
                SocialPostId = row.Source == "instagram" ? FyrInstagramPostId : row.Source == "x" ? FyrXPostId : null,
                Stage = row.Stage, Source = row.Source, Medium = row.Medium,
                LandingPath = "/servicios", Count = row.Count, OccurredAt = now.AddDays(-(index + 1)),
                DataSource = "simulated", CreatedAt = now,
            }));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
