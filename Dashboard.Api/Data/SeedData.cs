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
    public static readonly Guid FyrSeoOpportunityId = Guid.Parse("11111111-eeee-4111-8111-111111111111");
    public static readonly Guid FyrSeoUpdateOpportunityId = Guid.Parse("11111111-ffff-4111-8111-111111111111");
    public static readonly Guid FyrContentPieceId = Guid.Parse("11111111-1234-4111-8111-111111111111");
    public static readonly Guid FyrScheduledContentId = Guid.Parse("11111111-5678-4111-8111-111111111111");
    public static readonly Guid FyrRatePlanId = Guid.Parse("11111111-9001-4111-8111-111111111111");
    public static readonly Guid FyrHistoricalRatePlanId = Guid.Parse("11111111-9002-4111-8111-111111111111");
    public static readonly Guid FyrBudgetId = Guid.Parse("11111111-9003-4111-8111-111111111111");
    public static readonly Guid FyrSucceededExecutionId = Guid.Parse("11111111-9101-4111-8111-111111111111");
    public static readonly Guid FyrApprovalExecutionId = Guid.Parse("11111111-9102-4111-8111-111111111111");
    public static readonly Guid FyrFailedExecutionId = Guid.Parse("11111111-9103-4111-8111-111111111111");

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

        if (!await db.SeoOpportunities.AnyAsync(item => item.ProjectId == FyrStudiosId, cancellationToken))
        {
            var now = DateTimeOffset.UtcNow;
            db.SeoOpportunities.AddRange(
                new SeoOpportunity
                {
                    Id = FyrSeoOpportunityId, ProjectId = FyrStudiosId, Query = "diseño de identidad visual guatemala",
                    TargetPage = "/blog/identidad-visual", Evidence = "Consulta simulada con 1,240 impresiones y CTR de 1.1%.",
                    Hypothesis = "Una guía útil con ejemplos locales puede mejorar relevancia y clics cualificados.",
                    Status = "selected", DataSource = "simulated", BaselineImpressions = 1240, BaselineClicks = 14,
                    IsDemoData = true, CreatedAt = now.AddDays(-18), UpdatedAt = now.AddDays(-3),
                },
                new SeoOpportunity
                {
                    Id = FyrSeoUpdateOpportunityId, ProjectId = FyrStudiosId, Query = "cuánto cuesta crear una marca",
                    TargetPage = "/servicios/branding", Evidence = "Página existente con impresiones crecientes y contenido breve.",
                    Hypothesis = "Actualizar la página con proceso, rangos y preguntas frecuentes puede responder mejor la intención.",
                    Status = "selected", DataSource = "simulated", BaselineImpressions = 680, BaselineClicks = 18,
                    IsDemoData = true, CreatedAt = now.AddDays(-12), UpdatedAt = now.AddDays(-2),
                });

            var reviewPiece = new ContentPiece
            {
                Id = FyrContentPieceId, ProjectId = FyrStudiosId, SeoOpportunityId = FyrSeoOpportunityId,
                Title = "Guía para construir una identidad visual coherente", Slug = "guia-identidad-visual-coherente",
                ContentType = "new", PrimaryKeyword = "identidad visual guatemala", SearchIntent = "informational",
                Hypothesis = "La guía puede captar búsquedas informativas y conducir a una consulta de branding.",
                BaselineSummary = "1,240 impresiones, 14 clics y CTR aproximado de 1.1% en datos simulados.",
                Objective = "Superar 2% de CTR y registrar contactos cualificados después de 45 días.", Owner = "Carlos",
                Brief = "Explicar sistema visual, errores frecuentes, proceso práctico y criterios para contratar apoyo profesional.",
                DraftMarkdown = "# Identidad visual coherente\n\nUna identidad visual no es solo un logotipo. Es un sistema que ayuda a reconocer y confiar en una marca.",
                MetaTitle = "Identidad visual coherente: guía práctica", MetaDescription = "Aprende a construir una identidad visual consistente y útil para tu negocio.",
                Status = "pending_review", ResultNotes = string.Empty, SimulatedWordPressUrl = string.Empty,
                IsDemoData = true, CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-1),
            };
            reviewPiece.History.Add(new EditorialHistory
            {
                Id = Guid.NewGuid(), ProjectId = FyrStudiosId, ContentPieceId = FyrContentPieceId,
                FromStatus = "draft", ToStatus = "pending_review", Note = "Borrador simulado listo para revisión.",
                Actor = "Administrador local", ChangedAt = now.AddDays(-1),
            });

            var scheduledPiece = new ContentPiece
            {
                Id = FyrScheduledContentId, ProjectId = FyrStudiosId, SeoOpportunityId = FyrSeoUpdateOpportunityId,
                Title = "Qué incluye un proyecto de branding", Slug = "que-incluye-proyecto-branding",
                ContentType = "update", PrimaryKeyword = "proyecto de branding", SearchIntent = "commercial",
                Hypothesis = "Aclarar entregables y proceso puede mejorar la calidad de las solicitudes.",
                BaselineSummary = "680 impresiones y 18 clics en datos simulados.",
                Objective = "Actualizar la landing y revisar clics y contactos a los 45 días.", Owner = "Carlos",
                Brief = "Actualizar la sección de servicios con entregables, etapas, preguntas frecuentes y CTA.",
                DraftMarkdown = "# Qué incluye un proyecto de branding\n\nUn proyecto de branding alinea estrategia, identidad y aplicaciones.",
                MetaTitle = "Qué incluye un proyecto de branding", MetaDescription = "Conoce etapas, entregables y decisiones de un proceso profesional de branding.",
                Status = "scheduled", ScheduledFor = now.AddDays(7), ResultNotes = string.Empty,
                SimulatedWordPressUrl = string.Empty, IsDemoData = true, CreatedAt = now.AddDays(-8), UpdatedAt = now,
            };
            scheduledPiece.History.Add(new EditorialHistory
            {
                Id = Guid.NewGuid(), ProjectId = FyrStudiosId, ContentPieceId = FyrScheduledContentId,
                FromStatus = "approved", ToStatus = "scheduled", Note = "Programación editorial simulada.",
                Actor = "Administrador local", ChangedAt = now,
            });
            db.ContentPieces.AddRange(reviewPiece, scheduledPiece);
        }

        if (!await db.ApiRatePlans.AnyAsync(item => item.ProjectId == FyrStudiosId, cancellationToken))
        {
            var now = DateTimeOffset.UtcNow;
            var historicalRate = new ApiRatePlan
            {
                Id = FyrHistoricalRatePlanId, ProjectId = FyrStudiosId, Provider = "gemini", Model = "gemini-flash-demo",
                InputUsdPerMillion = 0.08m, OutputUsdPerMillion = 0.30m, EffectiveFrom = now.AddMonths(-3),
                IsActive = false, IsDemoData = true, CreatedAt = now.AddMonths(-3),
            };
            var activeRate = new ApiRatePlan
            {
                Id = FyrRatePlanId, ProjectId = FyrStudiosId, Provider = "gemini", Model = "gemini-flash-demo",
                InputUsdPerMillion = 0.10m, OutputUsdPerMillion = 0.40m, EffectiveFrom = now.AddMonths(-1),
                IsActive = true, IsDemoData = true, CreatedAt = now.AddMonths(-1),
            };
            db.ApiRatePlans.AddRange(historicalRate, activeRate);
            db.ProjectBudgets.Add(new ProjectBudget
            {
                Id = FyrBudgetId, ProjectId = FyrStudiosId, DailyLimitUsd = 2m, MonthlyLimitUsd = 25m,
                WarningPercent = 75, ExchangeRateGtqPerUsd = 7.75m, IsPaused = false, UpdatedAt = now,
            });

            var succeeded = new ExecutionRecord
            {
                Id = FyrSucceededExecutionId, ProjectId = FyrStudiosId, ApiRatePlanId = FyrRatePlanId,
                IdempotencyKey = "demo-seo-brief-001", Provider = "gemini", Model = "gemini-flash-demo",
                Flow = "seo_brief", Status = "succeeded", ApprovalRequired = true, ApprovedBy = "Administrador local",
                ApprovedAt = now.AddDays(-2).AddMinutes(2), InputUnits = 8400, OutputUnits = 2100,
                EstimatedCostUsd = 0.00168m, EstimatedCostGtq = 0.01302m, AttemptNumber = 1,
                ErrorCode = string.Empty, ErrorMessage = string.Empty, IsDemoData = true,
                CreatedAt = now.AddDays(-2), StartedAt = now.AddDays(-2).AddMinutes(3), CompletedAt = now.AddDays(-2).AddMinutes(4),
            };
            succeeded.Audit.Add(new ExecutionAudit
            {
                Id = Guid.NewGuid(), ProjectId = FyrStudiosId, ExecutionRecordId = succeeded.Id,
                EventType = "planned", FromStatus = string.Empty, ToStatus = "awaiting_approval",
                Note = "Estimación creada con tarifa versionada.", Actor = "Sistema local", OccurredAt = succeeded.CreatedAt,
            });
            succeeded.Audit.Add(new ExecutionAudit
            {
                Id = Guid.NewGuid(), ProjectId = FyrStudiosId, ExecutionRecordId = succeeded.Id,
                EventType = "approved", FromStatus = "awaiting_approval", ToStatus = "approved",
                Note = "Aprobación manual registrada.", Actor = "Administrador local", OccurredAt = succeeded.ApprovedAt!.Value,
            });
            succeeded.Audit.Add(new ExecutionAudit
            {
                Id = Guid.NewGuid(), ProjectId = FyrStudiosId, ExecutionRecordId = succeeded.Id,
                EventType = "completed", FromStatus = "approved", ToStatus = "succeeded",
                Note = "Ejecución simulada completada correctamente.", Actor = "Simulador local", OccurredAt = succeeded.CompletedAt!.Value,
            });

            var awaitingApproval = new ExecutionRecord
            {
                Id = FyrApprovalExecutionId, ProjectId = FyrStudiosId, ApiRatePlanId = FyrRatePlanId,
                IdempotencyKey = "demo-seo-draft-002", Provider = "gemini", Model = "gemini-flash-demo",
                Flow = "seo_draft", Status = "awaiting_approval", ApprovalRequired = true, ApprovedBy = string.Empty,
                InputUnits = 32000, OutputUnits = 8500, EstimatedCostUsd = 0.00660m, EstimatedCostGtq = 0.05115m,
                AttemptNumber = 1, ErrorCode = string.Empty, ErrorMessage = string.Empty, IsDemoData = true,
                CreatedAt = now.AddHours(-6),
            };
            awaitingApproval.Audit.Add(new ExecutionAudit
            {
                Id = Guid.NewGuid(), ProjectId = FyrStudiosId, ExecutionRecordId = awaitingApproval.Id,
                EventType = "planned", FromStatus = string.Empty, ToStatus = "awaiting_approval",
                Note = "Pendiente de aprobación antes de cualquier ejecución.", Actor = "Sistema local", OccurredAt = awaitingApproval.CreatedAt,
            });

            var failed = new ExecutionRecord
            {
                Id = FyrFailedExecutionId, ProjectId = FyrStudiosId, ApiRatePlanId = FyrRatePlanId,
                IdempotencyKey = "demo-content-failure-003", Provider = "gemini", Model = "gemini-flash-demo",
                Flow = "content_analysis", Status = "failed", ApprovalRequired = false, ApprovedBy = "Política local",
                ApprovedAt = now.AddDays(-1), InputUnits = 12000, OutputUnits = 3000,
                EstimatedCostUsd = 0.00240m, EstimatedCostGtq = 0.01860m, AttemptNumber = 1,
                ErrorCode = "SIMULATED_PROVIDER_ERROR", ErrorMessage = "Fallo de proveedor simulado para validar el reintento.",
                IsDemoData = true, CreatedAt = now.AddDays(-1), StartedAt = now.AddDays(-1).AddMinutes(1),
                CompletedAt = now.AddDays(-1).AddMinutes(2),
            };
            failed.Audit.Add(new ExecutionAudit
            {
                Id = Guid.NewGuid(), ProjectId = FyrStudiosId, ExecutionRecordId = failed.Id,
                EventType = "failed", FromStatus = "approved", ToStatus = "failed",
                Note = failed.ErrorMessage, Actor = "Simulador local", OccurredAt = failed.CompletedAt!.Value,
            });
            db.Executions.AddRange(succeeded, awaitingApproval, failed);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
