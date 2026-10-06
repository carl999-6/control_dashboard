using Dashboard.Api.Data;
using Dashboard.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Api.Services;

public sealed class AutomationExecutor(DashboardDbContext db, INotificationChannel notificationChannel, SearchConsoleService searchConsoleService, GeminiSeoService geminiSeoService, WordPressService wordPressService, XAssistantService xAssistantService, PostHogService postHogService)
{
    public async Task<AutomationRun> RunAsync(AutomationSchedule schedule, string trigger, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var project = await db.Projects.SingleAsync(item => item.Id == schedule.ProjectId, cancellationToken);
        var startOfDay = AutomationScheduleCalculator.StartOfLocalDayUtc(project.TimeZone, now);
        var runsToday = (await db.AutomationRuns.AsNoTracking()
            .Where(item => item.AutomationScheduleId == schedule.Id).ToListAsync(cancellationToken))
            .Count(item => item.CreatedAt >= startOfDay && item.Status != "blocked");

        var run = new AutomationRun
        {
            Id = Guid.NewGuid(), ProjectId = schedule.ProjectId, AutomationScheduleId = schedule.Id,
            Trigger = trigger, Status = "running", ErrorCode = string.Empty, ErrorMessage = string.Empty,
            CreatedAt = now, StartedAt = now,
        };
        db.AutomationRuns.Add(run);

        if (runsToday >= schedule.MaxRunsPerDay)
        {
            Complete(run, "blocked", "DAILY_LIMIT_REACHED", "La automatización alcanzó el límite diario configurado.");
        }
        else if (schedule.Workflow == "search_console_sync")
        {
            await RunSearchConsole(schedule, run, project, cancellationToken);
        }
        else if (schedule.Workflow == "seo_content_pipeline")
        {
            await RunGeminiSeo(schedule, run, project, cancellationToken);
        }
        else if (schedule.Workflow == "x_response_pipeline")
        {
            await RunXAssistant(schedule, run, project, cancellationToken);
        }
        else if (schedule.Workflow == "posthog_sync")
        {
            await RunPostHog(schedule, run, project, cancellationToken);
        }
        else if (schedule.Workflow is "telegram_daily_summary" or "telegram_weekly_summary")
        {
            await RunSummary(schedule, run, project, cancellationToken);
        }
        else
        {
            Complete(run, "blocked", "CONNECTOR_PENDING", "El conector real de este flujo se incorporará en la siguiente entrega de la fase 7.");
        }

        schedule.LastRunAt = now;
        schedule.NextRunAt = AutomationScheduleCalculator.CalculateNext(schedule, project.TimeZone, now);
        schedule.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return run;
    }

    private async Task RunXAssistant(AutomationSchedule schedule, AutomationRun run, Project project, CancellationToken cancellationToken)
    {
        var result = await xAssistantService.RunAsync(project.Id, cancellationToken);
        if (result.Succeeded)
        {
            Complete(run, "succeeded", string.Empty, string.Empty);
            await RecordNotification(project, schedule.Workflow, "Propuesta para X lista para revisión",
                $"Post original:\n{result.SourceUrl}\n\n1️⃣ Recomendada\n{result.RecommendedReply}\n\n" +
                $"2️⃣ Alternativa 1\n{result.AlternativeOne}\n\n" +
                $"3️⃣ Alternativa 2\n{result.AlternativeTwo}\n\n" +
                $"Los botones de Telegram copian solo el texto. El botón Responder en X del dashboard agrega también el enlace UTM. Lecturas importadas: {result.ImportedPosts}; costo X estimado: USD {result.EstimatedXCostUsd:0.000000}.",
                "info", cancellationToken, "gemini / x", result.Model, result.InputTokens, result.OutputTokens, result.EstimatedXCostUsd);
            return;
        }

        var blocked = result.ErrorCode is "X_ASSISTANT_DISABLED" or "X_BEARER_TOKEN_NOT_CONFIGURED" or "X_SEARCH_QUERY_REQUIRED" or
            "NO_X_OPPORTUNITY" or "GEMINI_KEY_NOT_CONFIGURED" or "GEMINI_DISABLED" or "GEMINI_DAILY_X_LIMIT" or
            "GEMINI_SHARED_DAILY_LIMIT" or "GEMINI_FREE_QUOTA_EXHAUSTED" or "BUDGET_BLOCKED" or "X_CREDITS_EXHAUSTED";
        Complete(run, blocked ? "blocked" : "failed", result.ErrorCode, result.ErrorMessage);
        if (result.ErrorCode is "GEMINI_FREE_QUOTA_EXHAUSTED" or "X_CREDITS_EXHAUSTED" or "BUDGET_BLOCKED")
        {
            await RecordNotification(project, schedule.Workflow, "Flujo de X detenido por cuota o presupuesto",
                $"{result.ErrorMessage} No se publicó ninguna respuesta.", "warning", cancellationToken,
                result.ErrorCode.StartsWith("GEMINI", StringComparison.Ordinal) ? "gemini" : "x", result.Model,
                result.InputTokens, result.OutputTokens, result.EstimatedXCostUsd);
        }
        else if (!blocked)
        {
            await RecordNotification(project, schedule.Workflow, "Falló el asistente de X",
                $"{result.ErrorMessage} No se publicó ninguna respuesta.", "critical", cancellationToken,
                "gemini / x", result.Model, result.InputTokens, result.OutputTokens, result.EstimatedXCostUsd);
        }
    }

    private async Task RunPostHog(AutomationSchedule schedule, AutomationRun run, Project project, CancellationToken cancellationToken)
    {
        var result = await postHogService.SyncAsync(project.Id, cancellationToken);
        if (result.Succeeded)
        {
            Complete(run, "succeeded", string.Empty, string.Empty);
            await RecordNotification(project, schedule.Workflow, "Sincronización de PostHog terminada",
                $"Importados {result.ImportedRows:N0} segmentos, {result.Pageviews:N0} páginas vistas, {result.QuoteRequests:N0} solicitudes de cotización y {result.WhatsAppClicks:N0} clics en WhatsApp.",
                "info", cancellationToken, "posthog", "web-analytics");
            return;
        }
        var blocked = result.ErrorCode is "POSTHOG_NOT_CONNECTED" or "POSTHOG_KEY_NOT_CONFIGURED" or "POSTHOG_ALREADY_RUNNING";
        Complete(run, blocked ? "blocked" : "failed", result.ErrorCode, result.ErrorMessage);
        if (!blocked)
            await RecordNotification(project, schedule.Workflow, "Falló la sincronización de PostHog",
                result.ErrorMessage, "critical", cancellationToken, "posthog", "web-analytics");
    }

    private async Task RunGeminiSeo(AutomationSchedule schedule, AutomationRun run, Project project, CancellationToken cancellationToken)
    {
        var result = await geminiSeoService.RunAsync(project.Id, cancellationToken);
        if (result.Succeeded)
        {
            var wordPress = await wordPressService.CreateDraftAsync(project.Id, result.ContentPieceId!.Value,
                "gemini", result.Model, result.InputTokens, result.OutputTokens, cancellationToken);
            if (wordPress.Succeeded)
            {
                Complete(run, "succeeded", string.Empty, string.Empty);
                return;
            }
            var configurationBlocked = wordPress.ErrorCode is "WORDPRESS_DISABLED" or "WORDPRESS_NOT_CONFIGURED";
            Complete(run, configurationBlocked ? "blocked" : "failed", wordPress.ErrorCode, wordPress.ErrorMessage);
            return;
        }

        var blocked = result.ErrorCode is "GEMINI_DISABLED" or "GEMINI_KEY_NOT_CONFIGURED" or "GEMINI_DAILY_DRAFT_LIMIT" or
            "NO_SEARCH_CONSOLE_DATA" or "NO_ELIGIBLE_SEO_OPPORTUNITY" or "GEMINI_FREE_QUOTA_EXHAUSTED" or "GEMINI_SHARED_DAILY_LIMIT";
        Complete(run, blocked ? "blocked" : "failed", result.ErrorCode, result.ErrorMessage);
        if (result.ErrorCode == "GEMINI_FREE_QUOTA_EXHAUSTED")
        {
            await RecordNotification(project, schedule.Workflow, "Cuota gratuita de Gemini agotada",
                "El flujo se detuvo sin utilizar ningún proveedor ni modelo pagado. Se reanudará únicamente en una ejecución posterior cuando la cuota vuelva a estar disponible.",
                "warning", cancellationToken, "gemini", result.Model, result.InputTokens, result.OutputTokens, 0);
        }
        else if (!blocked)
        {
            await RecordNotification(project, schedule.Workflow, "Falló la generación del borrador SEO",
                result.ErrorMessage, "critical", cancellationToken, "gemini", result.Model, result.InputTokens, result.OutputTokens, 0);
        }
    }

    private async Task RunSearchConsole(AutomationSchedule schedule, AutomationRun run, Project project, CancellationToken cancellationToken)
    {
        var result = await searchConsoleService.SyncAsync(project.Id, cancellationToken);
        if (result.Succeeded)
        {
            Complete(run, "succeeded", string.Empty, string.Empty);
            await RecordNotification(project, schedule.Workflow, "Sincronización de Search Console terminada",
                $"Se importaron {result.ImportedRows:N0} filas, {result.Clicks:N0} clics y {result.Impressions:N0} impresiones del periodo {result.StartDate:yyyy-MM-dd} a {result.EndDate:yyyy-MM-dd}.",
                "info", cancellationToken);
            return;
        }

        var blocked = result.ErrorCode is "SEARCH_CONSOLE_NOT_CONNECTED" or "SEARCH_CONSOLE_CLIENT_NOT_CONFIGURED" or "SEARCH_CONSOLE_REAUTHORIZATION_REQUIRED";
        Complete(run, blocked ? "blocked" : "failed", result.ErrorCode, result.ErrorMessage);
        if (!blocked)
        {
            await RecordNotification(project, schedule.Workflow, "Falló la sincronización de Search Console",
                result.ErrorMessage, "critical", cancellationToken);
        }
    }

    private async Task RunSummary(AutomationSchedule schedule, AutomationRun run, Project project, CancellationToken cancellationToken)
    {
        var since = schedule.Workflow == "telegram_weekly_summary"
            ? DateTimeOffset.UtcNow.AddDays(-7)
            : DateTimeOffset.UtcNow.AddDays(-1);
        var executions = (await db.Executions.AsNoTracking()
            .Where(item => item.ProjectId == project.Id).ToListAsync(cancellationToken))
            .Where(item => item.CreatedAt >= since).ToList();
        var notifications = (await db.Notifications.AsNoTracking()
            .Where(item => item.ProjectId == project.Id).ToListAsync(cancellationToken))
            .Where(item => item.CreatedAt >= since).ToList();
        var policy = await db.NotificationPolicies.SingleOrDefaultAsync(item => item.ProjectId == project.Id, cancellationToken);
        if (policy is null)
        {
            policy = new NotificationPolicy
            {
                Id = Guid.NewGuid(), ProjectId = project.Id, IsEnabled = true, DeliveryMode = notificationChannel.Mode,
                MinimumSeverity = "info", GroupWindowMinutes = 30, QuietHoursStart = 22, QuietHoursEnd = 7,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            db.NotificationPolicies.Add(policy);
        }
        if (!policy.IsEnabled)
        {
            Complete(run, "blocked", "NOTIFICATIONS_DISABLED", "Las notificaciones están desactivadas para este proyecto.");
            return;
        }

        policy.DeliveryMode = notificationChannel.Mode;
        var now = DateTimeOffset.UtcNow;
        var label = schedule.Workflow == "telegram_weekly_summary" ? "semanal" : "diario";
        var notification = new NotificationRecord
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, NotificationPolicyId = policy.Id,
            DeduplicationKey = $"{schedule.Workflow}-{now:yyyy-MM-dd}", Category = "summary", Severity = "info",
            Title = $"Resumen {label}",
            Message = $"Ejecuciones: {executions.Count}; completadas: {executions.Count(item => item.Status == "succeeded")}; fallidas: {executions.Count(item => item.Status == "failed")}; alertas fallidas: {notifications.Count(item => item.Status == "failed")}.",
            Flow = schedule.Workflow, Provider = "telegram", Model = string.Empty,
            Status = "queued", GroupCount = 1, AttemptCount = 1, SimulateFailure = false,
            ErrorMessage = string.Empty, IsDemoData = notificationChannel.Mode != "telegram",
            CreatedAt = now, UpdatedAt = now, LastAttemptAt = now,
        };
        if (policy.MinimumSeverity is "warning" or "critical")
        {
            notification.Status = "suppressed";
            notification.ErrorMessage = "La severidad no alcanza el umbral configurado.";
            db.Notifications.Add(notification);
            Complete(run, "succeeded", string.Empty, string.Empty);
            return;
        }
        if (IsQuietHour(policy, project.TimeZone, now))
        {
            notification.Status = "queued";
            notification.ErrorMessage = "En cola hasta que termine el horario silencioso.";
            db.Notifications.Add(notification);
            Complete(run, "succeeded", string.Empty, string.Empty);
            return;
        }
        var delivery = await notificationChannel.DeliverAsync(notification, project.Name, cancellationToken);
        notification.Status = delivery.Delivered ? "sent" : "failed";
        notification.DeliveredAt = delivery.Delivered ? now : null;
        notification.ErrorMessage = delivery.ErrorMessage;
        db.Notifications.Add(notification);
        Complete(run, delivery.Delivered ? "succeeded" : "failed",
            delivery.Delivered ? string.Empty : "TELEGRAM_DELIVERY_FAILED", delivery.ErrorMessage);
    }

    private static void Complete(AutomationRun run, string status, string errorCode, string errorMessage)
    {
        run.Status = status;
        run.ErrorCode = errorCode;
        run.ErrorMessage = errorMessage;
        run.CompletedAt = DateTimeOffset.UtcNow;
    }

    private async Task RecordNotification(Project project, string flow, string title, string message, string severity,
        CancellationToken cancellationToken, string provider = "google", string model = "search-console",
        int? inputUnits = null, int? outputUnits = null, decimal? estimatedCostUsd = null)
    {
        var policy = await db.NotificationPolicies.SingleOrDefaultAsync(item => item.ProjectId == project.Id, cancellationToken);
        if (policy is null)
        {
            policy = new NotificationPolicy
            {
                Id = Guid.NewGuid(), ProjectId = project.Id, IsEnabled = true, DeliveryMode = notificationChannel.Mode,
                MinimumSeverity = "info", GroupWindowMinutes = 30, QuietHoursStart = 22, QuietHoursEnd = 7,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            db.NotificationPolicies.Add(policy);
        }
        policy.DeliveryMode = notificationChannel.Mode;
        var now = DateTimeOffset.UtcNow;
        var notification = new NotificationRecord
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, NotificationPolicyId = policy.Id,
            DeduplicationKey = $"{flow}-{severity}-{now:yyyy-MM-dd-HH}", Category = "integration", Severity = severity,
            Title = title, Message = message, Flow = flow, Provider = provider, Model = model,
            InputUnits = inputUnits, OutputUnits = outputUnits, EstimatedCostUsd = estimatedCostUsd,
            Status = "queued", GroupCount = 1, AttemptCount = 0, SimulateFailure = false,
            ErrorMessage = string.Empty, IsDemoData = notificationChannel.Mode != "telegram",
            CreatedAt = now, UpdatedAt = now,
        };
        if (!policy.IsEnabled)
        {
            notification.Status = "suppressed";
            notification.ErrorMessage = "Las notificaciones están desactivadas para este proyecto.";
        }
        else if ((policy.MinimumSeverity == "critical" && severity != "critical") || (policy.MinimumSeverity == "warning" && severity == "info"))
        {
            notification.Status = "suppressed";
            notification.ErrorMessage = "La severidad no alcanza el umbral configurado.";
        }
        else if (IsQuietHour(policy, project.TimeZone, now))
        {
            notification.ErrorMessage = "En cola hasta que termine el horario silencioso.";
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

    private static bool IsQuietHour(NotificationPolicy policy, string timeZoneId, DateTimeOffset now)
    {
        if (policy.QuietHoursStart == policy.QuietHoursEnd) return false;
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { zone = TimeZoneInfo.Utc; }
        var hour = TimeZoneInfo.ConvertTime(now, zone).Hour;
        return policy.QuietHoursStart < policy.QuietHoursEnd
            ? hour >= policy.QuietHoursStart && hour < policy.QuietHoursEnd
            : hour >= policy.QuietHoursStart || hour < policy.QuietHoursEnd;
    }
}
