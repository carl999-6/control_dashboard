using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using Dashboard.Api.Domain;

namespace Dashboard.Api.Services;

public sealed class ConfigurableTelegramChannel(IConfiguration configuration, ILogger<ConfigurableTelegramChannel> logger)
    : INotificationChannel, IDisposable
{
    private readonly HttpClient _httpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
    }) { Timeout = TimeSpan.FromSeconds(ReadTimeoutSeconds(configuration)) };
    private readonly string _botToken = configuration["TELEGRAM_BOT_TOKEN"]?.Trim() ?? string.Empty;
    private readonly string _chatId = configuration["TELEGRAM_CHAT_ID"]?.Trim() ?? string.Empty;

    public string Mode => string.IsNullOrWhiteSpace(_botToken) || string.IsNullOrWhiteSpace(_chatId)
        ? "simulated"
        : "telegram";

    public async Task<NotificationDeliveryResult> DeliverAsync(
        NotificationRecord notification,
        string projectName,
        CancellationToken cancellationToken)
    {
        if (notification.SimulateFailure)
        {
            return new NotificationDeliveryResult(false, "Fallo simulado de Telegram; no se realizó ninguna llamada externa.");
        }

        if (Mode == "simulated")
        {
            return new NotificationDeliveryResult(true, string.Empty);
        }

        var text = BuildMessage(notification, projectName);
        var isXProposal = notification.Flow == "x_response_pipeline" && notification.Title == "Propuesta para X lista para revisión";
        object payload = isXProposal && TryBuildXReviewPayload(notification.Message, projectName, out var reviewPayload)
            ? new
            {
                chat_id = _chatId,
                text = reviewPayload.Html,
                parse_mode = "HTML",
                disable_web_page_preview = true,
                reply_markup = new
                {
                    inline_keyboard = new object[][]
                    {
                        [new { text = "📋 Copiar recomendada", copy_text = new { text = reviewPayload.Recommended } }],
                        [new { text = "📋 Copiar alternativa 1", copy_text = new { text = reviewPayload.AlternativeOne } }],
                        [new { text = "📋 Copiar alternativa 2", copy_text = new { text = reviewPayload.AlternativeTwo } }],
                    },
                },
            }
            : new { chat_id = _chatId, text, disable_web_page_preview = true };
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                $"https://api.telegram.org/bot{_botToken}/sendMessage",
                payload,
                cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new NotificationDeliveryResult(true, string.Empty);
            }

            logger.LogWarning("Telegram rechazó una notificación con estado HTTP {StatusCode}.", (int)response.StatusCode);
            return new NotificationDeliveryResult(false, $"Telegram respondió con estado HTTP {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new NotificationDeliveryResult(false, "Telegram agotó el tiempo de espera.");
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("Falló la conexión con Telegram; no se registró la URI para proteger el token del bot.");
            return new NotificationDeliveryResult(false, "No se pudo conectar con Telegram.");
        }
    }

    internal static string BuildMessage(NotificationRecord item, string projectName)
    {
        var builder = new StringBuilder();
        builder.AppendLine(item.Severity == "critical" ? "🚨 Alerta crítica" : item.Severity == "warning" ? "⚠️ Aviso" : "✅ Actividad");
        builder.AppendLine($"Proyecto: {projectName}");
        builder.AppendLine($"Evento: {item.Title}");
        if (!string.IsNullOrWhiteSpace(item.Flow)) builder.AppendLine($"Flujo: {item.Flow}");
        if (!string.IsNullOrWhiteSpace(item.Provider)) builder.AppendLine($"Proveedor: {item.Provider}");
        if (!string.IsNullOrWhiteSpace(item.Model)) builder.AppendLine($"Modelo: {item.Model}");
        if (item.InputUnits.HasValue) builder.AppendLine($"Tokens entrada: {item.InputUnits.Value:N0}");
        if (item.OutputUnits.HasValue) builder.AppendLine($"Tokens salida: {item.OutputUnits.Value:N0}");
        if (item.EstimatedCostUsd.HasValue) builder.AppendLine($"Costo estimado: USD {item.EstimatedCostUsd.Value.ToString("0.########", CultureInfo.InvariantCulture)}");
        builder.AppendLine($"Estado: {(item.Status == "queued" ? "enviado" : item.Status)}");
        builder.Append(item.Message);
        var result = builder.ToString();
        return result.Length <= 4096 ? result : result[..4093] + "…";
    }

    private static bool TryBuildXReviewPayload(string message, string projectName, out XReviewPayload payload)
    {
        var match = Regex.Match(message,
            @"^Post original:\r?\n(?<source>https://[^\r\n]+)\r?\n\r?\n1️⃣ Recomendada\r?\n(?<recommended>.*?)\r?\n\r?\n2️⃣ Alternativa 1\r?\n(?<one>.*?)\r?\n\r?\n3️⃣ Alternativa 2\r?\n(?<two>.*?)\r?\n\r?\n",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            payload = default!;
            return false;
        }

        var recommended = NormalizeCopyText(match.Groups["recommended"].Value);
        var alternativeOne = NormalizeCopyText(match.Groups["one"].Value);
        var alternativeTwo = NormalizeCopyText(match.Groups["two"].Value);
        if (new[] { recommended, alternativeOne, alternativeTwo }.Any(item => item.Length is < 1 or > 256))
        {
            payload = default!;
            return false;
        }

        var source = System.Net.WebUtility.HtmlEncode(match.Groups["source"].Value);
        var project = System.Net.WebUtility.HtmlEncode(projectName);
        var html = $"🎯 <b>Propuesta para X</b>\nProyecto: {project}\n\n🔗 <a href=\"{source}\">Abrir publicación original</a>\n\n" +
            $"1️⃣ <b>Recomendada</b>\n<pre>{System.Net.WebUtility.HtmlEncode(recommended)}</pre>\n\n" +
            $"2️⃣ <b>Alternativa 1</b>\n<pre>{System.Net.WebUtility.HtmlEncode(alternativeOne)}</pre>\n\n" +
            $"3️⃣ <b>Alternativa 2</b>\n<pre>{System.Net.WebUtility.HtmlEncode(alternativeTwo)}</pre>\n\n" +
            "Copia una respuesta, abre el post original, pégala y confirma la publicación en X.";
        payload = new XReviewPayload(html, recommended, alternativeOne, alternativeTwo);
        return true;
    }

    private static string NormalizeCopyText(string value)
    {
        var text = value.Trim();
        var tracking = text.IndexOf("\n\nMás información:", StringComparison.OrdinalIgnoreCase);
        if (tracking >= 0) text = text[..tracking].Trim();
        var legacyAction = text.IndexOf("\nResponder con esta opción:", StringComparison.OrdinalIgnoreCase);
        return legacyAction >= 0 ? text[..legacyAction].Trim() : text;
    }

    private static int ReadTimeoutSeconds(IConfiguration configuration)
    {
        return int.TryParse(configuration["TELEGRAM_TIMEOUT_SECONDS"], out var seconds)
            ? Math.Clamp(seconds, 5, 120)
            : 30;
    }

    private sealed record XReviewPayload(string Html, string Recommended, string AlternativeOne, string AlternativeTwo);

    public void Dispose() => _httpClient.Dispose();
}
