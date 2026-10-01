using Dashboard.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Data;

public static class SeedData
{
    public static readonly Guid FyrStudiosId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid FutureSaasId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    public static readonly Guid DemoClientId = Guid.Parse("33333333-3333-4333-8333-333333333333");

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

        await db.SaveChangesAsync(cancellationToken);
    }
}
