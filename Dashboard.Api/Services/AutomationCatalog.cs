namespace Dashboard.Api.Services;

public static class AutomationCatalog
{
    public static readonly IReadOnlyDictionary<string, string> Workflows = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["search_console_sync"] = "Sincronización diaria de Search Console",
        ["seo_content_pipeline"] = "Oportunidad SEO → borrador de WordPress",
        ["x_response_pipeline"] = "Propuestas de respuesta para X",
        ["posthog_sync"] = "Sincronización de analítica PostHog",
        ["telegram_daily_summary"] = "Resumen diario por Telegram",
        ["telegram_weekly_summary"] = "Resumen semanal por Telegram",
    };
}
