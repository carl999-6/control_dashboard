using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dashboard.Api.Contracts;
using Dashboard.Api.Data;
using Dashboard.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Services;

public sealed class GeminiSeoService(DashboardDbContext db, IGeminiApiClient apiClient, IConfiguration configuration)
{
    public const string DefaultModel = "gemini-3.5-flash-lite";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    public bool ApiKeyConfigured => !string.IsNullOrWhiteSpace(ApiKey);
    private string ApiKey => configuration["GEMINI_API_KEY"]?.Trim() ?? string.Empty;

    public async Task<GeminiSettingsResponse> GetSettingsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await db.Projects.AsNoTracking().SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken)
            ?? throw new KeyNotFoundException();
        var settings = await db.AiAutomationSettings.AsNoTracking().SingleOrDefaultAsync(item => item.ProjectId == projectId, cancellationToken);
        var startOfDay = AutomationScheduleCalculator.StartOfLocalDayUtc(project.TimeZone, DateTimeOffset.UtcNow);
        var generatedTodayItems = await db.Executions.AsNoTracking().Where(item => item.ProjectId == projectId &&
            item.Provider == "gemini" && item.Status == "succeeded").ToListAsync(cancellationToken);
        var today = generatedTodayItems.Where(item => item.CreatedAt >= startOfDay).ToList();
        return ToResponse(settings ?? Defaults(projectId), today.Count(item => item.Flow == "seo_content_pipeline"),
            today.Count(item => item.Flow == "x_response_pipeline"), today.Count, settings?.UpdatedAt);
    }

    public async Task<GeminiSettingsResponse> UpdateSettingsAsync(Guid projectId, GeminiSettingsRequest request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken)
            ?? throw new KeyNotFoundException();
        var settings = await db.AiAutomationSettings.SingleOrDefaultAsync(item => item.ProjectId == projectId, cancellationToken);
        if (settings is null)
        {
            settings = Defaults(projectId);
            db.AiAutomationSettings.Add(settings);
        }
        settings.IsEnabled = request.IsEnabled;
        settings.Model = request.Model.Trim();
        settings.MinimumImpressions = request.MinimumImpressions;
        settings.MinimumPosition = request.MinimumPosition;
        settings.MaximumPosition = request.MaximumPosition;
        settings.MaximumCtr = request.MaximumCtrPercent / 100d;
        settings.MaximumSeoDraftsPerDay = request.MaximumSeoDraftsPerDay;
        settings.MaximumXProposalsPerDay = request.MaximumXProposalsPerDay;
        settings.MaximumTotalGeminiRunsPerDay = request.MaximumTotalGeminiRunsPerDay;
        settings.MinimumDraftWords = request.MinimumDraftWords;
        settings.MaximumOutputTokens = request.MaximumOutputTokens;
        settings.Owner = request.Owner.Trim();
        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        var startOfDay = AutomationScheduleCalculator.StartOfLocalDayUtc(project.TimeZone, DateTimeOffset.UtcNow);
        var generatedTodayItems = await db.Executions.AsNoTracking().Where(item => item.ProjectId == projectId &&
            item.Provider == "gemini" && item.Status == "succeeded").ToListAsync(cancellationToken);
        var today = generatedTodayItems.Where(item => item.CreatedAt >= startOfDay).ToList();
        return ToResponse(settings, today.Count(item => item.Flow == "seo_content_pipeline"),
            today.Count(item => item.Flow == "x_response_pipeline"), today.Count, settings.UpdatedAt);
    }

    public async Task<GeminiSeoResult> RunAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await db.Projects.SingleAsync(item => item.Id == projectId, cancellationToken);
        var settings = await db.AiAutomationSettings.SingleOrDefaultAsync(item => item.ProjectId == projectId, cancellationToken) ?? Defaults(projectId);
        if (!settings.IsEnabled) return Failed("GEMINI_DISABLED", "La generación con Gemini está pausada desde el dashboard.", settings.Model);
        if (!ApiKeyConfigured) return Failed("GEMINI_KEY_NOT_CONFIGURED", "Falta GEMINI_API_KEY en el archivo .env local.", settings.Model);

        var startOfDay = AutomationScheduleCalculator.StartOfLocalDayUtc(project.TimeZone, DateTimeOffset.UtcNow);
        var generatedTodayItems = await db.Executions.AsNoTracking().Where(item => item.ProjectId == projectId &&
            item.Provider == "gemini" && item.Flow == "seo_content_pipeline" && item.Status == "succeeded").ToListAsync(cancellationToken);
        var generatedToday = generatedTodayItems.Count(item => item.CreatedAt >= startOfDay);
        if (generatedToday >= settings.MaximumSeoDraftsPerDay)
            return Failed("GEMINI_DAILY_DRAFT_LIMIT", "Se alcanzó el límite diario de borradores SEO configurado.", settings.Model);
        var allGeminiToday = (await db.Executions.AsNoTracking().Where(item => item.ProjectId == projectId &&
            item.Provider == "gemini" && item.Status == "succeeded").ToListAsync(cancellationToken)).Count(item => item.CreatedAt >= startOfDay);
        if (allGeminiToday >= settings.MaximumTotalGeminiRunsPerDay)
            return Failed("GEMINI_SHARED_DAILY_LIMIT", "Se alcanzó el límite diario compartido entre SEO y propuestas para X.", settings.Model);

        var metrics = await db.SearchConsoleMetrics.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(cancellationToken);
        if (metrics.Count == 0) return Failed("NO_SEARCH_CONSOLE_DATA", "Search Console todavía no tiene métricas para detectar oportunidades.", settings.Model);

        var opportunities = await db.SeoOpportunities.Where(item => item.ProjectId == projectId).ToListAsync(cancellationToken);
        var content = await db.ContentPieces.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(cancellationToken);
        var candidates = metrics.Where(item => !string.IsNullOrWhiteSpace(item.Query))
            .GroupBy(item => new { item.Query, item.Page })
            .Select(group =>
            {
                var impressions = group.Sum(item => item.Impressions);
                var clicks = group.Sum(item => item.Clicks);
                var position = impressions == 0 ? group.Average(item => item.Position) : group.Sum(item => item.Position * item.Impressions) / impressions;
                return new Candidate(group.Key.Query.Trim(), group.Key.Page.Trim(), impressions, clicks,
                    impressions == 0 ? 0 : clicks / impressions, position);
            })
            .Where(item => item.Impressions >= settings.MinimumImpressions && item.Position >= settings.MinimumPosition &&
                item.Position <= settings.MaximumPosition && item.Ctr <= settings.MaximumCtr)
            .OrderByDescending(item => item.Impressions)
            .ThenBy(item => item.Position)
            .ToList();

        var candidate = candidates.FirstOrDefault(item => !HasContentDuplicate(item.Query, content));
        if (candidate is null) return Failed("NO_ELIGIBLE_SEO_OPPORTUNITY", "No hay oportunidades nuevas que superen los umbrales y la comprobación de duplicados.", settings.Model);

        var opportunity = opportunities.FirstOrDefault(item => Normalize(item.Query) == Normalize(candidate.Query));
        if (opportunity is not null && content.Any(item => item.SeoOpportunityId == opportunity.Id))
            return Failed("NO_ELIGIBLE_SEO_OPPORTUNITY", "La oportunidad disponible ya tiene una pieza editorial asociada.", settings.Model);

        var now = DateTimeOffset.UtcNow;
        if (opportunity is null)
        {
            opportunity = new SeoOpportunity
            {
                Id = Guid.NewGuid(), ProjectId = projectId, Query = candidate.Query, TargetPage = candidate.Page,
                Evidence = $"Search Console: {candidate.Impressions:N0} impresiones, {candidate.Clicks:N0} clics, CTR {candidate.Ctr:P1}, posición media {candidate.Position:N1}.",
                Hypothesis = "Crear o mejorar contenido específico puede aumentar la relevancia, el CTR y las visitas orgánicas para esta consulta.",
                Status = "selected", DataSource = "google_search_console",
                BaselineImpressions = (int)Math.Round(candidate.Impressions), BaselineClicks = (int)Math.Round(candidate.Clicks),
                IsDemoData = false, CreatedAt = now, UpdatedAt = now,
            };
            db.SeoOpportunities.Add(opportunity);
        }
        else
        {
            opportunity.Status = "selected";
            opportunity.UpdatedAt = now;
        }

        var rate = await EnsureFreeRateAsync(projectId, settings.Model, now, cancellationToken);
        var execution = new ExecutionRecord
        {
            Id = Guid.NewGuid(), ProjectId = projectId, ApiRatePlanId = rate.Id,
            IdempotencyKey = $"gemini-seo-{opportunity.Id:N}", Provider = "gemini", Model = settings.Model,
            Flow = "seo_content_pipeline", Status = "running", ApprovalRequired = false,
            ApprovedBy = "Política local de cuota gratuita", ApprovedAt = now, InputUnits = 0, OutputUnits = 0,
            EstimatedCostUsd = 0, EstimatedCostGtq = 0, AttemptNumber = 1, ErrorCode = string.Empty,
            ErrorMessage = string.Empty, IsDemoData = false, CreatedAt = now, StartedAt = now,
        };
        execution.Audit.Add(CreateAudit(projectId, execution.Id, "started", string.Empty, "running", "Solicitud real a Gemini iniciada con política de cuota gratuita.", now));
        db.Executions.Add(execution);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var generation = await apiClient.GenerateAsync(ApiKey, settings.Model, BuildPrompt(project, opportunity, candidate, settings), settings.MaximumOutputTokens, cancellationToken);
            var payload = ParsePayload(generation.Text);
            var validationError = ValidatePayload(payload, settings.MinimumDraftWords);
            if (validationError is not null)
            {
                CompleteExecution(execution, "failed", "GEMINI_DRAFT_VALIDATION_FAILED", validationError, generation.InputTokens, generation.OutputTokens);
                db.ExecutionAudit.Add(CreateAudit(projectId, execution.Id, "validation_failed", "running", "failed", validationError, DateTimeOffset.UtcNow));
                await db.SaveChangesAsync(cancellationToken);
                return Failed("GEMINI_DRAFT_VALIDATION_FAILED", validationError, settings.Model, opportunity.Id, generation.InputTokens, generation.OutputTokens);
            }

            var completedAt = DateTimeOffset.UtcNow;
            var piece = new ContentPiece
            {
                Id = Guid.NewGuid(), ProjectId = projectId, SeoOpportunityId = opportunity.Id,
                Title = payload!.Title.Trim(), Slug = NormalizeSlug(payload.Title), ContentType = NormalizeContentType(payload.ContentType),
                PrimaryKeyword = candidate.Query, SearchIntent = NormalizeIntent(payload.SearchIntent), Hypothesis = payload.Hypothesis.Trim(),
                BaselineSummary = opportunity.Evidence, Objective = payload.Objective.Trim(), Owner = settings.Owner,
                Brief = payload.Brief.Trim(), DraftMarkdown = payload.DraftMarkdown.Trim(), MetaTitle = payload.MetaTitle.Trim(),
                MetaDescription = payload.MetaDescription.Trim(), Status = "draft", ResultNotes = string.Empty,
                SimulatedWordPressUrl = string.Empty, IsDemoData = false, CreatedAt = completedAt, UpdatedAt = completedAt,
            };
            piece.History.Add(new EditorialHistory
            {
                Id = Guid.NewGuid(), ProjectId = projectId, ContentPieceId = piece.Id, FromStatus = string.Empty,
                ToStatus = "draft", Note = "Borrador generado por Gemini y validado automáticamente; requiere revisión humana.",
                Actor = "Automatización Gemini", ChangedAt = completedAt,
            });
            db.ContentPieces.Add(piece);
            opportunity.Status = "converted";
            opportunity.UpdatedAt = completedAt;
            CompleteExecution(execution, "succeeded", string.Empty, string.Empty, generation.InputTokens, generation.OutputTokens);
            db.ExecutionAudit.Add(CreateAudit(projectId, execution.Id, "completed", "running", "succeeded", "Borrador generado y guardado para revisión humana; no se publicó contenido.", completedAt));
            await db.SaveChangesAsync(cancellationToken);
            return new GeminiSeoResult(true, string.Empty, string.Empty, opportunity.Id, piece.Id, piece.Title,
                settings.Model, generation.InputTokens, generation.OutputTokens);
        }
        catch (GeminiApiException exception)
        {
            CompleteExecution(execution, exception.Code == "GEMINI_FREE_QUOTA_EXHAUSTED" ? "blocked" : "failed",
                exception.Code, exception.Message, 0, 0);
            db.ExecutionAudit.Add(CreateAudit(projectId, execution.Id, "failed", "running", execution.Status, exception.Message, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync(cancellationToken);
            return Failed(exception.Code, exception.Message, settings.Model, opportunity.Id);
        }
        catch (HttpRequestException)
        {
            const string message = "No se pudo contactar a Gemini.";
            CompleteExecution(execution, "failed", "GEMINI_UNAVAILABLE", message, 0, 0);
            db.ExecutionAudit.Add(CreateAudit(projectId, execution.Id, "failed", "running", "failed", message, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync(cancellationToken);
            return Failed("GEMINI_UNAVAILABLE", message, settings.Model, opportunity.Id);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            const string message = "Gemini no respondió dentro del tiempo esperado.";
            CompleteExecution(execution, "failed", "GEMINI_TIMEOUT", message, 0, 0);
            db.ExecutionAudit.Add(CreateAudit(projectId, execution.Id, "failed", "running", "failed", message, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync(cancellationToken);
            return Failed("GEMINI_TIMEOUT", message, settings.Model, opportunity.Id);
        }
    }

    private async Task<ApiRatePlan> EnsureFreeRateAsync(Guid projectId, string model, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var rates = await db.ApiRatePlans.Where(item => item.ProjectId == projectId && item.Provider == "gemini" && item.Model == model && item.IsActive).ToListAsync(cancellationToken);
        var free = rates.FirstOrDefault(item => item.InputUsdPerMillion == 0 && item.OutputUsdPerMillion == 0);
        if (free is not null) return free;
        foreach (var item in rates) item.IsActive = false;
        free = new ApiRatePlan
        {
            Id = Guid.NewGuid(), ProjectId = projectId, Provider = "gemini", Model = model,
            InputUsdPerMillion = 0, OutputUsdPerMillion = 0, EffectiveFrom = now,
            IsActive = true, IsDemoData = false, CreatedAt = now,
        };
        db.ApiRatePlans.Add(free);
        return free;
    }

    private static string BuildPrompt(Project project, SeoOpportunity opportunity, Candidate candidate, AiAutomationSettings settings) => $$"""
        Actúa como estratega SEO y redactor profesional para {{project.Name}} ({{project.Domain}}).
        Genera un borrador en español que aporte valor real y que siempre quede pendiente de revisión humana.
        No inventes clientes, testimonios, estadísticas, precios, certificaciones ni capacidades no proporcionadas.
        No afirmes que el texto ya fue publicado. No incluyas HTML; usa Markdown.

        Consulta objetivo: {{candidate.Query}}
        Página observada: {{candidate.Page}}
        Evidencia: {{opportunity.Evidence}}
        Objetivo: cubrir la intención de búsqueda y mejorar relevancia y CTR sin sobreoptimización.
        Extensión mínima del borrador: {{settings.MinimumDraftWords}} palabras.

        Devuelve exclusivamente un objeto JSON válido con estas propiedades de texto:
        title, contentType, searchIntent, hypothesis, objective, brief, draftMarkdown, metaTitle, metaDescription.
        contentType debe ser new, update o merge. searchIntent debe ser informational, commercial, transactional o navigational.
        metaTitle debe tener máximo 70 caracteres y metaDescription máximo 170 caracteres.
        """;

    private static GeneratedDraft? ParsePayload(string text)
    {
        var cleaned = text.Trim();
        if (cleaned.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = cleaned.IndexOf('\n');
            var lastFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewLine >= 0 && lastFence > firstNewLine) cleaned = cleaned[(firstNewLine + 1)..lastFence].Trim();
        }
        try { return JsonSerializer.Deserialize<GeneratedDraft>(cleaned, JsonOptions); }
        catch (JsonException) { return null; }
    }

    private static string? ValidatePayload(GeneratedDraft? payload, int minimumWords)
    {
        if (payload is null) return "La respuesta no contiene JSON válido.";
        if (string.IsNullOrWhiteSpace(payload.Title) || payload.Title.Trim().Length > 220) return "El título generado no es válido.";
        if (string.IsNullOrWhiteSpace(payload.ContentType) || string.IsNullOrWhiteSpace(payload.SearchIntent)) return "Gemini no indicó el tipo de contenido o la intención de búsqueda.";
        if (string.IsNullOrWhiteSpace(payload.DraftMarkdown)) return "Gemini no generó el cuerpo del borrador.";
        var words = Regex.Matches(payload.DraftMarkdown, @"\b[\p{L}\p{N}][\p{L}\p{N}'’-]*\b").Count;
        if (words < minimumWords) return $"El borrador contiene {words} palabras y el mínimo configurado es {minimumWords}.";
        if (string.IsNullOrWhiteSpace(payload.MetaTitle) || payload.MetaTitle.Trim().Length > 70) return "El meta título generado supera 70 caracteres o está vacío.";
        if (string.IsNullOrWhiteSpace(payload.MetaDescription) || payload.MetaDescription.Trim().Length > 170) return "La meta descripción supera 170 caracteres o está vacía.";
        if (string.IsNullOrWhiteSpace(payload.Hypothesis) || string.IsNullOrWhiteSpace(payload.Objective) || string.IsNullOrWhiteSpace(payload.Brief)) return "El borrador no incluye todos los campos editoriales obligatorios.";
        return null;
    }

    private static bool HasContentDuplicate(string query, IReadOnlyCollection<ContentPiece> content)
    {
        var normalized = Normalize(query);
        return content.Any(item => IsSimilar(normalized, Normalize(item.PrimaryKeyword)) || IsSimilar(normalized, Normalize(item.Title)));
    }

    private static bool IsSimilar(string left, string right) => left == right ||
        (left.Length >= 8 && right.Length >= 8 && (left.Contains(right, StringComparison.Ordinal) || right.Contains(left, StringComparison.Ordinal)));

    private static string Normalize(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return Regex.Replace(decomposed, @"[^a-z0-9]+", " ").Trim();
    }

    private static string NormalizeSlug(string value) => Regex.Replace(Normalize(value), @"\s+", "-").Trim('-');
    private static string NormalizeContentType(string value) => value.Trim().ToLowerInvariant() is "update" or "merge" ? value.Trim().ToLowerInvariant() : "new";
    private static string NormalizeIntent(string value) => value.Trim().ToLowerInvariant() is "commercial" or "transactional" or "navigational" ? value.Trim().ToLowerInvariant() : "informational";

    private static void CompleteExecution(ExecutionRecord execution, string status, string code, string message, int input, int output)
    {
        execution.Status = status; execution.ErrorCode = code; execution.ErrorMessage = message;
        execution.InputUnits = input; execution.OutputUnits = output; execution.CompletedAt = DateTimeOffset.UtcNow;
    }

    private static ExecutionAudit CreateAudit(Guid projectId, Guid executionId, string type, string from, string to, string note, DateTimeOffset at) => new()
    {
        Id = Guid.NewGuid(), ProjectId = projectId, ExecutionRecordId = executionId, EventType = type,
        FromStatus = from, ToStatus = to, Note = note, Actor = "Automatización local", OccurredAt = at,
    };

    private static AiAutomationSettings Defaults(Guid projectId) => new()
    {
        Id = Guid.NewGuid(), ProjectId = projectId, IsEnabled = true, Model = DefaultModel,
        MinimumImpressions = 20, MinimumPosition = 4, MaximumPosition = 30, MaximumCtr = 0.05,
        MaximumSeoDraftsPerDay = 1, MaximumXProposalsPerDay = 1, MaximumTotalGeminiRunsPerDay = 2,
        MinimumDraftWords = 700, MaximumOutputTokens = 4096,
        Owner = "Carlos", UpdatedAt = DateTimeOffset.UtcNow,
    };

    private GeminiSettingsResponse ToResponse(AiAutomationSettings settings, int generatedToday, int xGeneratedToday, int totalToday, DateTimeOffset? updatedAt) => new(
        ApiKeyConfigured, settings.IsEnabled, settings.Model, settings.MinimumImpressions,
        settings.MinimumPosition, settings.MaximumPosition, settings.MaximumCtr * 100,
        settings.MaximumSeoDraftsPerDay, settings.MaximumXProposalsPerDay, settings.MaximumTotalGeminiRunsPerDay,
        settings.MinimumDraftWords, settings.MaximumOutputTokens,
        settings.Owner, generatedToday, xGeneratedToday, totalToday, updatedAt);

    private static GeminiSeoResult Failed(string code, string message, string model, Guid? opportunityId = null, int input = 0, int output = 0) =>
        new(false, code, message, opportunityId, null, string.Empty, model, input, output);

    private sealed record Candidate(string Query, string Page, double Impressions, double Clicks, double Ctr, double Position);
    private sealed record GeneratedDraft(string Title, string ContentType, string SearchIntent, string Hypothesis,
        string Objective, string Brief, string DraftMarkdown, string MetaTitle, string MetaDescription);
}
