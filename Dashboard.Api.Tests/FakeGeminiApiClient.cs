using System.Text.Json;
using Dashboard.Api.Services;

namespace Dashboard.Api.Tests;

public sealed class FakeGeminiApiClient : IGeminiApiClient
{
    public Task<GeminiGeneration> GenerateAsync(string apiKey, string model, string prompt, int maximumOutputTokens, CancellationToken cancellationToken)
    {
        if (prompt.Contains("asistente de conversación", StringComparison.OrdinalIgnoreCase))
        {
            var replyPayload = JsonSerializer.Serialize(new
            {
                recommendedReply = "Buen punto. Definir el objetivo y las prioridades antes de diseñar ayuda a tomar decisiones más claras durante todo el proyecto.",
                alternativeOne = "Coincido: empezar por el problema que debe resolver el sitio evita diseñar por preferencia y permite evaluar mejor cada decisión.",
                alternativeTwo = "Una buena base es acordar objetivo, audiencia y acción principal. Con eso, diseño y contenido pueden trabajar en la misma dirección.",
                rationale = "Aporta una idea concreta relacionada con el post sin promocionar servicios ni inventar resultados.",
                riskNotes = "Revisar el contexto del hilo antes de responder.",
            });
            return Task.FromResult(new GeminiGeneration(replyPayload, 310, 190, "STOP"));
        }
        var body = string.Join(' ', Enumerable.Repeat("Contenido útil para explicar servicios creativos, resolver preguntas reales y orientar una decisión informada.", 38));
        var payload = JsonSerializer.Serialize(new
        {
            title = "Cómo elegir servicios de diseño web en Guatemala",
            contentType = "new",
            searchIntent = "commercial",
            hypothesis = "Una guía clara puede responder la consulta y mejorar la relevancia orgánica.",
            objective = "Ayudar al lector a evaluar un servicio de diseño web sin afirmaciones inventadas.",
            brief = "Explicar criterios, proceso, preguntas frecuentes y siguientes pasos.",
            draftMarkdown = $"# Cómo elegir servicios de diseño web en Guatemala\n\n{body}",
            metaTitle = "Cómo elegir diseño web en Guatemala",
            metaDescription = "Guía para evaluar servicios de diseño web en Guatemala, comparar procesos y preparar un proyecto con objetivos claros.",
        });
        return Task.FromResult(new GeminiGeneration(payload, 640, 980, "STOP"));
    }
}
