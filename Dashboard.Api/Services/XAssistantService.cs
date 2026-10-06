using System.Text.Json;
using System.Text.RegularExpressions;
using Dashboard.Api.Contracts;
using Dashboard.Api.Data;
using Dashboard.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Services;

public sealed class XAssistantService(
    DashboardDbContext db,
    IXApiClient xApiClient,
    IGeminiApiClient geminiApiClient,
    IConfiguration configuration)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    private static readonly HashSet<string> CountedBudgetStatuses = ["approved", "running", "succeeded", "failed"];
    private string BearerToken => configuration["X_BEARER_TOKEN"]?.Trim() ?? string.Empty;
    private string GeminiApiKey => configuration["GEMINI_API_KEY"]?.Trim() ?? string.Empty;

    public async Task<XAssistantSettingsResponse> GetSettingsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await db.Projects.AnyAsync(item => item.Id == projectId, cancellationToken)) throw new KeyNotFoundException();
        var settings = await db.XAssistantSettings.AsNoTracking().SingleOrDefaultAsync(item => item.ProjectId == projectId, cancellationToken) ?? Defaults(projectId);
        return ToResponse(settings);
    }

    public async Task<XAssistantSettingsResponse> UpdateSettingsAsync(Guid projectId, XAssistantSettingsRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Projects.AnyAsync(item => item.Id == projectId, cancellationToken)) throw new KeyNotFoundException();
        var settings = await db.XAssistantSettings.SingleOrDefaultAsync(item => item.ProjectId == projectId, cancellationToken);
        if (settings is null)
        {
            settings = Defaults(projectId);
            db.XAssistantSettings.Add(settings);
        }
        settings.IsEnabled = request.IsEnabled;
        settings.ApiReadEnabled = request.ApiReadEnabled;
        settings.SearchQuery = request.SearchQuery.Trim();
        settings.Language = request.Language.Trim().ToLowerInvariant();
        settings.MaximumPostsPerSync = request.MaximumPostsPerSync;
        settings.ReadCostUsdPerPost = request.ReadCostUsdPerPost;
        settings.ToneInstructions = request.ToneInstructions.Trim();
        settings.LandingPath = NormalizeLandingPath(request.LandingPath);
        settings.UtmCampaign = NormalizeUtm(request.UtmCampaign);
        settings.LastError = string.Empty;
        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(settings);
    }

    public async Task<XSourcePost> ImportManualAsync(Guid projectId, XSourcePostRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Projects.AnyAsync(item => item.Id == projectId, cancellationToken)) throw new KeyNotFoundException();
        var externalId = ExtractPostId(request.Url);
        var existing = await db.XSourcePosts.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.ExternalPostId == externalId, cancellationToken);
        if (existing is not null) return existing;
        var now = DateTimeOffset.UtcNow;
        var item = new XSourcePost
        {
            Id = Guid.NewGuid(), ProjectId = projectId, ExternalPostId = externalId, Url = request.Url.Trim(),
            AuthorUsername = request.AuthorUsername.Trim().TrimStart('@'), Text = request.Text.Trim(),
            Language = request.Language.Trim().ToLowerInvariant(), LikeCount = request.LikeCount,
            ReplyCount = request.ReplyCount, RepostCount = request.RepostCount, QuoteCount = request.QuoteCount,
            ImpressionCount = request.ImpressionCount, Status = "detected", DataSource = "manual",
            PostedAt = request.PostedAt ?? now, ImportedAt = now,
        };
        db.XSourcePosts.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<XAssistantRunResult> RunAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await db.Projects.SingleAsync(item => item.Id == projectId, cancellationToken);
        var settings = await db.XAssistantSettings.SingleOrDefaultAsync(item => item.ProjectId == projectId, cancellationToken) ?? Defaults(projectId);
        if (!settings.IsEnabled) return Failed("X_ASSISTANT_DISABLED", "El asistente de X está pausado.");

        var candidate = await FindCandidateAsync(projectId, cancellationToken);
        var imported = 0;
        var xCost = 0m;
        if (candidate is null && settings.ApiReadEnabled)
        {
            var sync = await SyncRecentAsync(project, settings, cancellationToken);
            if (!sync.Succeeded) return Failed(sync.ErrorCode, sync.ErrorMessage, importedPosts: sync.ImportedPosts, xCost: sync.EstimatedCostUsd);
            imported = sync.ImportedPosts;
            xCost = sync.EstimatedCostUsd;
            candidate = await FindCandidateAsync(projectId, cancellationToken);
        }
        if (candidate is null) return Failed("NO_X_OPPORTUNITY", "No hay publicaciones pendientes. Importa una URL manualmente o habilita la búsqueda de pago.", importedPosts: imported, xCost: xCost);
        if (string.IsNullOrWhiteSpace(GeminiApiKey)) return Failed("GEMINI_KEY_NOT_CONFIGURED", "Falta GEMINI_API_KEY en .env.", importedPosts: imported, xCost: xCost);

        var aiSettings = await GetOrCreateAiSettingsAsync(projectId, cancellationToken);
        if (!aiSettings.IsEnabled) return Failed("GEMINI_DISABLED", "Gemini está pausado desde el dashboard.", aiSettings.Model, importedPosts: imported, xCost: xCost);
        var startOfDay = AutomationScheduleCalculator.StartOfLocalDayUtc(project.TimeZone, DateTimeOffset.UtcNow);
        var todayExecutions = (await db.Executions.AsNoTracking().Where(item => item.ProjectId == projectId && item.Provider == "gemini" && item.Status == "succeeded").ToListAsync(cancellationToken))
            .Where(item => item.CreatedAt >= startOfDay).ToList();
        if (todayExecutions.Count(item => item.Flow == "x_response_pipeline") >= aiSettings.MaximumXProposalsPerDay)
            return Failed("GEMINI_DAILY_X_LIMIT", "Se alcanzó el límite diario de propuestas para X.", aiSettings.Model, importedPosts: imported, xCost: xCost);
        if (todayExecutions.Count >= aiSettings.MaximumTotalGeminiRunsPerDay)
            return Failed("GEMINI_SHARED_DAILY_LIMIT", "Se alcanzó el límite diario compartido entre SEO y propuestas para X.", aiSettings.Model, importedPosts: imported, xCost: xCost);

        var now = DateTimeOffset.UtcNow;
        var rate = await EnsureRateAsync(projectId, "gemini", aiSettings.Model, 0, now, cancellationToken);
        var budget = await GetOrCreateBudgetAsync(projectId, cancellationToken);
        var execution = CreateExecution(project, rate, $"gemini-x-{candidate.Id:N}", "gemini", aiSettings.Model,
            "x_response_pipeline", 0, 0, budget.ExchangeRateGtqPerUsd, now);
        execution.Audit.Add(CreateAudit(projectId, execution.Id, "started", string.Empty, "running",
            "Generación de propuestas para X iniciada; la salida quedará pendiente de revisión humana.", now));
        db.Executions.Add(execution);
        await db.SaveChangesAsync(cancellationToken);
        try
        {
            var generation = await geminiApiClient.GenerateAsync(GeminiApiKey, aiSettings.Model,
                BuildPrompt(project, candidate, settings), Math.Min(aiSettings.MaximumOutputTokens, 2048), cancellationToken);
            var payload = JsonSerializer.Deserialize<GeneratedReplies>(StripCodeFence(generation.Text), JsonOptions);
            var validation = ValidatePayload(payload);
            if (validation is not null)
            {
                CompleteExecution(execution, "failed", "GEMINI_X_VALIDATION_FAILED", validation, generation.InputTokens, generation.OutputTokens, 0, budget.ExchangeRateGtqPerUsd);
                db.ExecutionAudit.Add(CreateAudit(projectId, execution.Id, "validation_failed", "running", "failed", validation, DateTimeOffset.UtcNow));
                await db.SaveChangesAsync(cancellationToken);
                return Failed("GEMINI_X_VALIDATION_FAILED", validation, aiSettings.Model, generation.InputTokens, generation.OutputTokens, imported, xCost);
            }
            var proposalId = Guid.NewGuid();
            var proposal = new XReplyProposal
            {
                Id = proposalId, ProjectId = projectId, XSourcePostId = candidate.Id,
                RecommendedReply = payload!.RecommendedReply.Trim(), AlternativeOne = payload.AlternativeOne.Trim(),
                AlternativeTwo = payload.AlternativeTwo.Trim(), SelectedReply = payload.RecommendedReply.Trim(),
                Rationale = payload.Rationale.Trim(), RiskNotes = payload.RiskNotes?.Trim() ?? string.Empty,
                Status = "pending_review", PublishedReplyUrl = string.Empty,
                TrackingUrl = BuildTrackingUrl(project, settings, proposalId), CreatedAt = now, UpdatedAt = now,
            };
            db.XReplyProposals.Add(proposal);
            candidate.Status = "proposed";
            CompleteExecution(execution, "succeeded", string.Empty, string.Empty, generation.InputTokens, generation.OutputTokens, 0, budget.ExchangeRateGtqPerUsd);
            db.ExecutionAudit.Add(CreateAudit(projectId, execution.Id, "completed", "running", "succeeded",
                "Tres propuestas generadas para revisión; no se publicó nada en X.", DateTimeOffset.UtcNow));
            await db.SaveChangesAsync(cancellationToken);
            return new(true, string.Empty, string.Empty, proposal.Id, candidate.Url, aiSettings.Model,
                generation.InputTokens, generation.OutputTokens, imported, xCost, proposal.RecommendedReply,
                proposal.AlternativeOne, proposal.AlternativeTwo, proposal.TrackingUrl);
        }
        catch (GeminiApiException exception)
        {
            var status = exception.Code == "GEMINI_FREE_QUOTA_EXHAUSTED" ? "blocked" : "failed";
            CompleteExecution(execution, status, exception.Code, exception.Message, 0, 0, 0, budget.ExchangeRateGtqPerUsd);
            db.ExecutionAudit.Add(CreateAudit(projectId, execution.Id, "failed", "running", status, exception.Message, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync(cancellationToken);
            return Failed(exception.Code, exception.Message, aiSettings.Model, importedPosts: imported, xCost: xCost);
        }
        catch (HttpRequestException exception)
        {
            const string message = "No se pudo contactar a Gemini.";
            CompleteExecution(execution, "failed", "GEMINI_NETWORK_ERROR", message, 0, 0, 0, budget.ExchangeRateGtqPerUsd);
            db.ExecutionAudit.Add(CreateAudit(projectId, execution.Id, "failed", "running", "failed", exception.Message, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync(cancellationToken);
            return Failed("GEMINI_NETWORK_ERROR", message, aiSettings.Model, importedPosts: imported, xCost: xCost);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            const string message = "Gemini excedió el tiempo de espera.";
            CompleteExecution(execution, "failed", "GEMINI_TIMEOUT", message, 0, 0, 0, budget.ExchangeRateGtqPerUsd);
            db.ExecutionAudit.Add(CreateAudit(projectId, execution.Id, "failed", "running", "failed", message, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync(cancellationToken);
            return Failed("GEMINI_TIMEOUT", message, aiSettings.Model, importedPosts: imported, xCost: xCost);
        }
    }

    public async Task<XSyncResult> SyncRecentAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await db.Projects.SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken)
            ?? throw new KeyNotFoundException();
        var settings = await db.XAssistantSettings.SingleOrDefaultAsync(item => item.ProjectId == projectId, cancellationToken) ?? Defaults(projectId);
        if (!settings.ApiReadEnabled)
            return new(false, "X_API_READ_DISABLED", "La lectura pagada de X está desactivada.", 0, 0, 0);
        return await SyncRecentAsync(project, settings, cancellationToken);
    }

    private async Task<XSourcePost?> FindCandidateAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var detected = await db.XSourcePosts.Where(item => item.ProjectId == projectId && item.Status == "detected")
            .ToListAsync(cancellationToken);
        var staleThreshold = DateTimeOffset.UtcNow.AddDays(-7);
        var stale = detected.Where(item => item.DataSource == "x_api" && item.PostedAt < staleThreshold).ToList();
        foreach (var item in stale) item.Status = "dismissed";
        if (stale.Count > 0) await db.SaveChangesAsync(cancellationToken);
        return detected.Except(stale).OrderByDescending(Score).ThenByDescending(item => item.PostedAt).FirstOrDefault();
    }

    private async Task<XSyncResult> SyncRecentAsync(Project project, XAssistantSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(BearerToken)) return new(false, "X_BEARER_TOKEN_NOT_CONFIGURED", "Falta X_BEARER_TOKEN en .env.", 0, 0, 0);
        if (string.IsNullOrWhiteSpace(settings.SearchQuery)) return new(false, "X_SEARCH_QUERY_REQUIRED", "Configura una búsqueda antes de habilitar lecturas pagadas.", 0, 0, 0);
        var maximumCost = settings.MaximumPostsPerSync * settings.ReadCostUsdPerPost;
        var budget = await GetOrCreateBudgetAsync(project.Id, cancellationToken);
        if (!await FitsBudgetAsync(project, budget, maximumCost, cancellationToken))
            return new(false, "BUDGET_BLOCKED", "El costo máximo de la consulta supera el presupuesto disponible.", 0, 0, 0);

        var now = DateTimeOffset.UtcNow;
        var rate = await EnsureRateAsync(project.Id, "x", "recent-search", settings.ReadCostUsdPerPost, now, cancellationToken);
        var execution = CreateExecution(project, rate, $"x-search-{now:yyyyMMddHHmm}-{Guid.NewGuid():N}", "x", "recent-search",
            "x_response_pipeline", settings.MaximumPostsPerSync, maximumCost, budget.ExchangeRateGtqPerUsd, now);
        execution.Audit.Add(CreateAudit(project.Id, execution.Id, "started", string.Empty, "running",
            $"Consulta reciente de solo lectura iniciada con costo máximo estimado de USD {maximumCost:0.000000}.", now));
        db.Executions.Add(execution);
        await db.SaveChangesAsync(cancellationToken);
        try
        {
            var query = settings.SearchQuery;
            if (!query.Contains("lang:", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(settings.Language)) query += $" lang:{settings.Language}";
            if (!query.Contains("-is:retweet", StringComparison.OrdinalIgnoreCase)) query += " -is:retweet";
            var posts = await xApiClient.SearchRecentAsync(BearerToken, query, settings.MaximumPostsPerSync, cancellationToken);
            var imported = 0;
            foreach (var post in posts)
            {
                if (await db.XSourcePosts.AnyAsync(item => item.ProjectId == project.Id && item.ExternalPostId == post.Id, cancellationToken)) continue;
                db.XSourcePosts.Add(new XSourcePost
                {
                    Id = Guid.NewGuid(), ProjectId = project.Id, ExternalPostId = post.Id, Url = post.Url,
                    AuthorUsername = post.AuthorUsername, Text = post.Text, Language = post.Language,
                    LikeCount = post.LikeCount, ReplyCount = post.ReplyCount, RepostCount = post.RepostCount,
                    QuoteCount = post.QuoteCount, ImpressionCount = post.ImpressionCount, Status = "detected",
                    DataSource = "x_api", PostedAt = post.CreatedAt, ImportedAt = now,
                });
                imported++;
            }
            var actualCost = posts.Count * settings.ReadCostUsdPerPost;
            CompleteExecution(execution, "succeeded", string.Empty, string.Empty, posts.Count, 0, actualCost, budget.ExchangeRateGtqPerUsd);
            db.ExecutionAudit.Add(CreateAudit(project.Id, execution.Id, "completed", "running", "succeeded",
                $"Se leyeron {posts.Count} posts y se importaron {imported}; no se escribió nada en X.", DateTimeOffset.UtcNow));
            settings.LastSyncAt = now;
            settings.LastError = string.Empty;
            settings.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            return new(true, string.Empty, string.Empty, posts.Count, imported, actualCost);
        }
        catch (XApiException exception)
        {
            CompleteExecution(execution, "failed", exception.Code, exception.Message, 0, 0, 0, budget.ExchangeRateGtqPerUsd);
            db.ExecutionAudit.Add(CreateAudit(project.Id, execution.Id, "failed", "running", "failed", exception.Message, DateTimeOffset.UtcNow));
            settings.LastError = exception.Message;
            settings.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return new(false, exception.Code, exception.Message, 0, 0, 0);
        }
    }

    private async Task<AiAutomationSettings> GetOrCreateAiSettingsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var settings = await db.AiAutomationSettings.SingleOrDefaultAsync(item => item.ProjectId == projectId, cancellationToken);
        if (settings is not null) return settings;
        settings = new AiAutomationSettings
        {
            Id = Guid.NewGuid(), ProjectId = projectId, IsEnabled = true, Model = GeminiSeoService.DefaultModel,
            MinimumImpressions = 20, MinimumPosition = 4, MaximumPosition = 30, MaximumCtr = .05,
            MaximumSeoDraftsPerDay = 1, MaximumXProposalsPerDay = 1, MaximumTotalGeminiRunsPerDay = 2,
            MinimumDraftWords = 700, MaximumOutputTokens = 4096, Owner = "Carlos", UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.AiAutomationSettings.Add(settings);
        return settings;
    }

    private async Task<ApiRatePlan> EnsureRateAsync(Guid projectId, string provider, string model, decimal unitCost, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var perMillion = unitCost * 1_000_000m;
        var active = await db.ApiRatePlans.Where(item => item.ProjectId == projectId && item.Provider == provider && item.Model == model && item.IsActive).ToListAsync(cancellationToken);
        var matching = active.FirstOrDefault(item => item.InputUsdPerMillion == perMillion && item.OutputUsdPerMillion == 0);
        if (matching is not null) return matching;
        foreach (var item in active) item.IsActive = false;
        var rate = new ApiRatePlan
        {
            Id = Guid.NewGuid(), ProjectId = projectId, Provider = provider, Model = model,
            InputUsdPerMillion = perMillion, OutputUsdPerMillion = 0, EffectiveFrom = now,
            IsActive = true, IsDemoData = false, CreatedAt = now,
        };
        db.ApiRatePlans.Add(rate);
        return rate;
    }

    private static ExecutionRecord CreateExecution(Project project, ApiRatePlan rate, string key, string provider, string model,
        string flow, int input, decimal cost, decimal exchangeRate, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), ProjectId = project.Id, ApiRatePlanId = rate.Id, IdempotencyKey = key,
        Provider = provider, Model = model, Flow = flow, Status = "running", ApprovalRequired = false,
        ApprovedBy = "Política local", ApprovedAt = now, InputUnits = input, OutputUnits = 0,
        EstimatedCostUsd = cost, EstimatedCostGtq = cost * exchangeRate, AttemptNumber = 1, ErrorCode = string.Empty,
        ErrorMessage = string.Empty, IsDemoData = false, CreatedAt = now, StartedAt = now,
    };

    private static void CompleteExecution(ExecutionRecord execution, string status, string code, string message,
        int input, int output, decimal costUsd, decimal exchangeRate)
    {
        execution.Status = status; execution.ErrorCode = code; execution.ErrorMessage = message;
        execution.InputUnits = input; execution.OutputUnits = output; execution.EstimatedCostUsd = costUsd;
        execution.EstimatedCostGtq = costUsd * exchangeRate;
        execution.CompletedAt = DateTimeOffset.UtcNow;
    }

    private static ExecutionAudit CreateAudit(Guid projectId, Guid executionId, string type, string from, string to, string note, DateTimeOffset at) => new()
    {
        Id = Guid.NewGuid(), ProjectId = projectId, ExecutionRecordId = executionId, EventType = type,
        FromStatus = from, ToStatus = to, Note = note, Actor = "Sistema", OccurredAt = at,
    };

    private async Task<ProjectBudget> GetOrCreateBudgetAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var budget = await db.ProjectBudgets.SingleOrDefaultAsync(item => item.ProjectId == projectId, cancellationToken);
        if (budget is not null) return budget;
        budget = new ProjectBudget
        {
            Id = Guid.NewGuid(), ProjectId = projectId, DailyLimitUsd = 5, MonthlyLimitUsd = 50,
            WarningPercent = 80, ExchangeRateGtqPerUsd = 7.75m, IsPaused = false, UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.ProjectBudgets.Add(budget);
        await db.SaveChangesAsync(cancellationToken);
        return budget;
    }

    private async Task<bool> FitsBudgetAsync(Project project, ProjectBudget budget, decimal extra, CancellationToken cancellationToken)
    {
        if (budget.IsPaused) return false;
        var today = AutomationScheduleCalculator.StartOfLocalDayUtc(project.TimeZone, DateTimeOffset.UtcNow);
        var local = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, FindTimeZone(project.TimeZone));
        var monthLocal = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var month = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(monthLocal, FindTimeZone(project.TimeZone)));
        var executions = await db.Executions.AsNoTracking().Where(item => item.ProjectId == project.Id).ToListAsync(cancellationToken);
        var counted = executions.Where(item => CountedBudgetStatuses.Contains(item.Status));
        return counted.Where(item => item.CreatedAt >= today).Sum(item => item.EstimatedCostUsd) + extra <= budget.DailyLimitUsd &&
            counted.Where(item => item.CreatedAt >= month).Sum(item => item.EstimatedCostUsd) + extra <= budget.MonthlyLimitUsd;
    }

    private static string BuildPrompt(Project project, XSourcePost source, XAssistantSettings settings) => $$"""
        Actúa como asistente de conversación para {{project.Name}}. Redacta respuestas humanas, útiles y específicas en español.
        Nunca afirmes datos que no aparecen en el post. No inventes experiencias, clientes, cifras ni resultados.
        No incluyas enlaces ni promoción forzada. No ataques, acoses ni uses contenido discriminatorio.
        Cada respuesta debe caber en 235 caracteres para reservar espacio a un enlace medible. No incluyas enlaces ni la mención del autor; el sistema aplica un formato uniforme y abre el compositor de respuesta de X.
        Toda salida quedará pendiente de revisión humana; no será publicada por la API.
        Estilo solicitado: {{settings.ToneInstructions}}
        Autor: {{(string.IsNullOrWhiteSpace(source.AuthorUsername) ? "cuenta no incluida en la lectura para evitar consultar recursos de usuario adicionales" : $"@{source.AuthorUsername}")}}
        Post: {{source.Text}}
        Métricas disponibles: {{source.LikeCount}} me gusta, {{source.ReplyCount}} respuestas, {{source.RepostCount}} republicaciones, {{source.QuoteCount}} citas.
        Devuelve exclusivamente JSON válido con: recommendedReply, alternativeOne, alternativeTwo, rationale, riskNotes.
        """;

    private static string? ValidatePayload(GeneratedReplies? payload)
    {
        if (payload is null) return "Gemini no devolvió una propuesta válida.";
        var replies = new[] { payload.RecommendedReply, payload.AlternativeOne, payload.AlternativeTwo };
        if (replies.Any(string.IsNullOrWhiteSpace)) return "Las tres alternativas son obligatorias.";
        if (replies.Any(item => item.EnumerateRunes().Count() > 235)) return "Una propuesta supera 235 caracteres y no deja espacio seguro para el enlace medible.";
        if (string.IsNullOrWhiteSpace(payload.Rationale)) return "Falta la explicación de relevancia.";
        return null;
    }

    private static double Score(XSourcePost item) =>
        (item.ImpressionCount / 100d + item.LikeCount + item.RepostCount * 2d + item.QuoteCount * 2d + 1d) / (item.ReplyCount + 1d);

    private static string BuildTrackingUrl(Project project, XAssistantSettings settings, Guid proposalId)
    {
        var root = project.Domain.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? project.Domain.TrimEnd('/') : $"https://{project.Domain.TrimEnd('/')}";
        var landing = string.IsNullOrWhiteSpace(settings.LandingPath) ? "/" : settings.LandingPath;
        var separator = landing.Contains('?') ? '&' : '?';
        return $"{root}{landing}{separator}utm_source=x&utm_medium=organic&utm_campaign={Uri.EscapeDataString(settings.UtmCampaign)}&utm_content=reply-{proposalId:N}";
    }

    public static string ExtractPostId(string url)
    {
        var match = Regex.Match(url?.Trim() ?? string.Empty, @"^https://(?:www\.)?(?:x|twitter)\.com/[A-Za-z0-9_]+/status/(\d+)(?:[/?#].*)?$", RegexOptions.IgnoreCase);
        if (!match.Success) throw new ArgumentException("Usa una URL válida de un post de X, por ejemplo https://x.com/usuario/status/123.");
        return match.Groups[1].Value;
    }

    public static string BuildReplyText(string reply, string trackingUrl) =>
        $"{reply.Trim()}\n\nMás información: {trackingUrl.Trim()}";

    public static string BuildReplyIntent(string sourceUrl, string reply, string trackingUrl)
    {
        var postId = ExtractPostId(sourceUrl);
        var text = Uri.EscapeDataString(BuildReplyText(reply, trackingUrl));
        return $"https://twitter.com/intent/tweet?in_reply_to={postId}&text={text}";
    }

    private static string NormalizeLandingPath(string value) => string.IsNullOrWhiteSpace(value) ? "/" : "/" + value.Trim().Trim('/');
    private static string NormalizeUtm(string value) => Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9_-]+", "-").Trim('-');
    private static string StripCodeFence(string value)
    {
        var text = value.Trim();
        if (!text.StartsWith("```", StringComparison.Ordinal)) return text;
        var firstBreak = text.IndexOf('\n');
        var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
        return firstBreak >= 0 && lastFence > firstBreak ? text[(firstBreak + 1)..lastFence].Trim() : text;
    }

    private static TimeZoneInfo FindTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }

    private XAssistantSettingsResponse ToResponse(XAssistantSettings item) => new(
        item.IsEnabled, item.ApiReadEnabled, !string.IsNullOrWhiteSpace(BearerToken), item.SearchQuery,
        item.Language, item.MaximumPostsPerSync, item.ReadCostUsdPerPost,
        item.MaximumPostsPerSync * item.ReadCostUsdPerPost, item.ToneInstructions, item.LandingPath,
        item.UtmCampaign, item.LastSyncAt, item.LastError);

    private static XAssistantSettings Defaults(Guid projectId) => new()
    {
        Id = Guid.NewGuid(), ProjectId = projectId, IsEnabled = true, ApiReadEnabled = false,
        SearchQuery = "diseño web OR branding OR marketing digital", Language = "es", MaximumPostsPerSync = 10,
        ReadCostUsdPerPost = .005m, ToneInstructions = "Profesional, cercano, concreto y útil; sin vender de forma agresiva.",
        LandingPath = "/servicios", UtmCampaign = "x-assistant", LastError = string.Empty, UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static XAssistantRunResult Failed(string code, string message, string model = "", int input = 0, int output = 0,
        int importedPosts = 0, decimal xCost = 0) => new(false, code, message, null, string.Empty, model, input, output,
            importedPosts, xCost, string.Empty, string.Empty, string.Empty, string.Empty);

    private sealed record GeneratedReplies(string RecommendedReply, string AlternativeOne, string AlternativeTwo, string Rationale, string? RiskNotes);
}
