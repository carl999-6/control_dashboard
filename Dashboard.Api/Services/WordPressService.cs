using System.Net;
using System.Text;
using Dashboard.Api.Contracts;
using Dashboard.Api.Data;
using Dashboard.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Services;

public sealed class WordPressService(
    DashboardDbContext db,
    IWordPressApiClient apiClient,
    INotificationChannel notificationChannel,
    IConfiguration configuration)
{
    private const string Provider = "wordpress";

    public async Task<WordPressSettingsResponse> GetSettingsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var connection = await GetOrCreateConnectionAsync(projectId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToSettings(connection);
    }

    public async Task<WordPressSettingsResponse> UpdateSettingsAsync(Guid projectId, WordPressSettingsRequest request, CancellationToken cancellationToken)
    {
        var connection = await GetOrCreateConnectionAsync(projectId, cancellationToken);
        connection.ResourceId = NormalizeBaseUrl(request.BaseUrl);
        connection.ExternalAccount = request.Username.Trim();
        connection.Status = request.IsEnabled ? "configured" : "paused";
        connection.LastError = string.Empty;
        connection.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToSettings(connection);
    }

    public async Task<WordPressConnectionTestResponse> TestConnectionAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var connection = await GetOrCreateConnectionAsync(projectId, cancellationToken);
        var password = configuration["WORDPRESS_APPLICATION_PASSWORD"]?.Trim() ?? string.Empty;
        if (connection.Status == "paused") return new(false, "paused", "La integración de WordPress está pausada.");
        if (string.IsNullOrWhiteSpace(password)) return new(false, "not_configured", "Falta WORDPRESS_APPLICATION_PASSWORD en .env.");
        try
        {
            await apiClient.ValidateAsync(connection.ResourceId, connection.ExternalAccount, password, cancellationToken);
            connection.Status = "connected";
            connection.LastSyncAt = DateTimeOffset.UtcNow;
            connection.LastError = string.Empty;
            connection.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return new(true, connection.Status, "WordPress confirmó las credenciales y el acceso de edición.");
        }
        catch (WordPressApiException exception)
        {
            connection.Status = "error";
            connection.LastSyncAt = DateTimeOffset.UtcNow;
            connection.LastError = exception.Message;
            connection.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return new(false, connection.Status, exception.Message);
        }
    }

    public async Task<WordPressDraftResult> CreateDraftAsync(Guid projectId, Guid contentPieceId,
        string aiProvider, string aiModel, int? inputTokens, int? outputTokens, CancellationToken cancellationToken)
    {
        var connection = await GetOrCreateConnectionAsync(projectId, cancellationToken);
        var piece = await db.ContentPieces.Include(item => item.History)
            .SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == contentPieceId, cancellationToken);
        if (piece is null) throw new KeyNotFoundException();
        if (piece.WordPressPostId.HasValue)
            return new(true, string.Empty, string.Empty, piece.Id, piece.WordPressPostId,
                piece.SimulatedWordPressUrl, true);

        var preflight = ValidatePreflight(connection, piece);
        if (preflight is not null)
        {
            await NotifyAsync(piece.ProjectId, "Flujo de WordPress detenido",
                $"El borrador local “{piece.Title}” se conservó. {preflight.ErrorMessage}",
                "warning", aiProvider, aiModel, inputTokens, outputTokens, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return preflight;
        }

        var execution = await StartExecutionAsync(projectId, piece.Id, cancellationToken);
        var password = configuration["WORDPRESS_APPLICATION_PASSWORD"]!.Trim();
        try
        {
            var created = await apiClient.CreateDraftAsync(connection.ResourceId, connection.ExternalAccount, password,
                new WordPressDraftPayload(piece.Title, piece.Slug, MarkdownToHtml(piece.DraftMarkdown), piece.MetaDescription), cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var previous = piece.Status;
            piece.WordPressPostId = created.Id;
            piece.WordPressStatus = "draft";
            piece.WordPressDraftCreatedAt = now;
            piece.SimulatedWordPressUrl = $"{connection.ResourceId}/wp-admin/post.php?post={created.Id}&action=edit";
            piece.Status = "sent_draft";
            piece.UpdatedAt = now;
            db.EditorialHistory.Add(new EditorialHistory
            {
                Id = Guid.NewGuid(), ProjectId = projectId, ContentPieceId = piece.Id,
                FromStatus = previous, ToStatus = "sent_draft", Actor = "Automatización local",
                Note = $"Borrador real #{created.Id} creado en WordPress con estado draft; no fue publicado.", ChangedAt = now,
            });
            CompleteExecution(execution, true, string.Empty, string.Empty);
            connection.Status = "connected";
            connection.LastSyncAt = now;
            connection.LastError = string.Empty;
            connection.UpdatedAt = now;
            await NotifyAsync(piece.ProjectId, "Borrador de WordPress creado",
                $"Se creó “{piece.Title}” como draft de WordPress (ID {created.Id}). Requiere revisión y publicación manual. {piece.SimulatedWordPressUrl}",
                "info", aiProvider, aiModel, inputTokens, outputTokens, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return new(true, string.Empty, string.Empty, piece.Id, created.Id, piece.SimulatedWordPressUrl, false);
        }
        catch (WordPressApiException exception)
        {
            CompleteExecution(execution, false, exception.Code, exception.Message);
            connection.Status = "error";
            connection.LastError = exception.Message;
            connection.UpdatedAt = DateTimeOffset.UtcNow;
            await NotifyAsync(piece.ProjectId, "Falló la creación del borrador en WordPress",
                $"El borrador local “{piece.Title}” se conservó para reintentar. {exception.Message}",
                "critical", aiProvider, aiModel, inputTokens, outputTokens, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return new(false, exception.Code, exception.Message, piece.Id, null, string.Empty, false);
        }
    }

    private WordPressDraftResult? ValidatePreflight(IntegrationConnection connection, ContentPiece piece)
    {
        if (connection.Status == "paused") return Failure(piece.Id, "WORDPRESS_DISABLED", "La integración de WordPress está pausada.");
        if (string.IsNullOrWhiteSpace(configuration["WORDPRESS_APPLICATION_PASSWORD"]))
            return Failure(piece.Id, "WORDPRESS_NOT_CONFIGURED", "Falta WORDPRESS_APPLICATION_PASSWORD en .env.");
        if (string.IsNullOrWhiteSpace(connection.ResourceId) || string.IsNullOrWhiteSpace(connection.ExternalAccount))
            return Failure(piece.Id, "WORDPRESS_NOT_CONFIGURED", "Configura la URL y el usuario de WordPress.");
        if (piece.Status is not ("draft" or "pending_review" or "approved" or "scheduled"))
            return Failure(piece.Id, "WORDPRESS_INVALID_CONTENT_STATUS", "La pieza no está en un estado que pueda enviarse como borrador.");
        if (string.IsNullOrWhiteSpace(piece.DraftMarkdown))
            return Failure(piece.Id, "WORDPRESS_EMPTY_DRAFT", "El contenido local no tiene un borrador para enviar.");
        return null;
    }

    private async Task<IntegrationConnection> GetOrCreateConnectionAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await db.Projects.SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken)
            ?? throw new KeyNotFoundException();
        var connection = await db.IntegrationConnections.SingleOrDefaultAsync(
            item => item.ProjectId == projectId && item.Provider == Provider, cancellationToken);
        if (connection is not null) return connection;
        var baseUrl = configuration["WORDPRESS_BASE_URL"]?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = project.Domain.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? project.Domain : $"https://{project.Domain}";
        var now = DateTimeOffset.UtcNow;
        connection = new IntegrationConnection
        {
            Id = Guid.NewGuid(), ProjectId = projectId, Provider = Provider, Status = "configured",
            ExternalAccount = configuration["WORDPRESS_USERNAME"]?.Trim() ?? string.Empty,
            ResourceId = NormalizeBaseUrl(baseUrl), EncryptedRefreshToken = string.Empty,
            GrantedScopes = "posts:draft-only", LookbackDays = 0, RowLimit = 0,
            LastError = string.Empty, CreatedAt = now, UpdatedAt = now,
        };
        db.IntegrationConnections.Add(connection);
        return connection;
    }

    private async Task<ExecutionRecord> StartExecutionAsync(Guid projectId, Guid contentId, CancellationToken cancellationToken)
    {
        var plan = await db.ApiRatePlans.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Provider == Provider && item.Model == "rest-api" && item.IsActive, cancellationToken);
        if (plan is null)
        {
            plan = new ApiRatePlan
            {
                Id = Guid.NewGuid(), ProjectId = projectId, Provider = Provider, Model = "rest-api",
                InputUsdPerMillion = 0, OutputUsdPerMillion = 0, EffectiveFrom = DateTimeOffset.UtcNow,
                IsActive = true, IsDemoData = false, CreatedAt = DateTimeOffset.UtcNow,
            };
            db.ApiRatePlans.Add(plan);
        }
        var keyPrefix = $"wordpress-draft-{contentId:N}";
        var previous = await db.Executions.Where(item => item.ProjectId == projectId && item.Provider == Provider && item.IdempotencyKey.StartsWith(keyPrefix))
            .OrderByDescending(item => item.AttemptNumber).FirstOrDefaultAsync(cancellationToken);
        var attempt = (previous?.AttemptNumber ?? 0) + 1;
        var execution = new ExecutionRecord
        {
            Id = Guid.NewGuid(), ProjectId = projectId, ApiRatePlanId = plan.Id,
            ParentExecutionId = previous?.Id, IdempotencyKey = attempt == 1 ? keyPrefix : $"{keyPrefix}-retry-{attempt}",
            Provider = Provider, Model = "rest-api",
            Flow = "seo_content_pipeline", Status = "running", ApprovalRequired = false,
            ApprovedBy = string.Empty, InputUnits = 0, OutputUnits = 0, EstimatedCostUsd = 0,
            EstimatedCostGtq = 0, AttemptNumber = attempt, ErrorCode = string.Empty, ErrorMessage = string.Empty,
            IsDemoData = false, CreatedAt = DateTimeOffset.UtcNow, StartedAt = DateTimeOffset.UtcNow,
        };
        db.Executions.Add(execution);
        return execution;
    }

    private async Task NotifyAsync(Guid projectId, string title, string message, string severity, string aiProvider,
        string aiModel, int? inputTokens, int? outputTokens, CancellationToken cancellationToken)
    {
        var project = await db.Projects.SingleAsync(item => item.Id == projectId, cancellationToken);
        var policy = await db.NotificationPolicies.SingleOrDefaultAsync(item => item.ProjectId == projectId, cancellationToken);
        if (policy is null)
        {
            policy = new NotificationPolicy
            {
                Id = Guid.NewGuid(), ProjectId = projectId, IsEnabled = true, DeliveryMode = notificationChannel.Mode,
                MinimumSeverity = "info", GroupWindowMinutes = 30, QuietHoursStart = 22, QuietHoursEnd = 7,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            db.NotificationPolicies.Add(policy);
        }
        var now = DateTimeOffset.UtcNow;
        var notification = new NotificationRecord
        {
            Id = Guid.NewGuid(), ProjectId = projectId, NotificationPolicyId = policy.Id,
            DeduplicationKey = $"wordpress-{severity}-{now:yyyyMMddHHmmss}", Category = "integration", Severity = severity,
            Title = title, Message = message, Flow = "seo_content_pipeline",
            Provider = string.IsNullOrWhiteSpace(aiProvider) ? Provider : $"{aiProvider} / {Provider}", Model = aiModel,
            InputUnits = inputTokens, OutputUnits = outputTokens, EstimatedCostUsd = 0,
            Status = "queued", GroupCount = 1, AttemptCount = 0, SimulateFailure = false,
            ErrorMessage = string.Empty, IsDemoData = notificationChannel.Mode != "telegram", CreatedAt = now, UpdatedAt = now,
        };
        if (!policy.IsEnabled)
        {
            notification.Status = "suppressed";
            notification.ErrorMessage = "Las notificaciones están desactivadas para este proyecto.";
        }
        else
        {
            notification.AttemptCount = 1;
            notification.LastAttemptAt = now;
            var delivery = await notificationChannel.DeliverAsync(notification, project.Name, cancellationToken);
            notification.Status = delivery.Delivered ? "sent" : "failed";
            notification.DeliveredAt = delivery.Delivered ? now : null;
            notification.ErrorMessage = delivery.ErrorMessage;
        }
        db.Notifications.Add(notification);
    }

    private WordPressSettingsResponse ToSettings(IntegrationConnection connection) => new(
        connection.Status != "paused", !string.IsNullOrWhiteSpace(configuration["WORDPRESS_APPLICATION_PASSWORD"]),
        connection.ResourceId, connection.ExternalAccount, connection.Status, connection.LastSyncAt, connection.LastError);

    private static void CompleteExecution(ExecutionRecord execution, bool succeeded, string code, string message)
    {
        execution.Status = succeeded ? "succeeded" : "failed";
        execution.ErrorCode = code;
        execution.ErrorMessage = message;
        execution.CompletedAt = DateTimeOffset.UtcNow;
    }

    private static WordPressDraftResult Failure(Guid contentId, string code, string message) =>
        new(false, code, message, contentId, null, string.Empty, false);

    private static string NormalizeBaseUrl(string value) => value.Trim().TrimEnd('/');

    private static string MarkdownToHtml(string markdown)
    {
        var blocks = new List<string>();
        foreach (var raw in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var line = WebUtility.HtmlEncode(raw.Trim());
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("### ")) blocks.Add($"<h3>{line[4..]}</h3>");
            else if (line.StartsWith("## ")) blocks.Add($"<h2>{line[3..]}</h2>");
            else if (line.StartsWith("# ")) blocks.Add($"<h1>{line[2..]}</h1>");
            else if (line.StartsWith("- ")) blocks.Add($"<p>• {line[2..]}</p>");
            else blocks.Add($"<p>{line}</p>");
        }
        return string.Join('\n', blocks);
    }
}
