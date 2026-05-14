using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TuCita.Application.Common;
using TuCita.Application.InformesInteligentes;

namespace TuCita.Infrastucture.InformesInteligentes;

internal sealed class GeminiInformeInteligenteClient(IOptions<GeminiOptions> options) : IInformeInteligenteAiClient
{
    private static readonly HttpClient HttpClient = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public async Task<ServiceResult<InformeAiGenerationResult>> GenerarInformeAsync(
        InformeInteligenteContextoDto contexto,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            return ServiceResult<InformeAiGenerationResult>.Validation([
                new ValidationError(nameof(GeminiOptions.Enabled), "La generación con Gemini no está habilitada.")
            ]);
        }

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return ServiceResult<InformeAiGenerationResult>.Validation([
                new ValidationError(nameof(GeminiOptions.ApiKey), "Gemini:ApiKey no está configurado.")
            ]);
        }

        if (string.IsNullOrWhiteSpace(settings.Model))
        {
            return ServiceResult<InformeAiGenerationResult>.Validation([
                new ValidationError(nameof(GeminiOptions.Model), "Gemini:Model no está configurado.")
            ]);
        }

        var endpoint = BuildEndpoint(settings);
        var prompt = BuildPrompt(contexto);
        var timeoutSeconds = Math.Clamp(settings.TimeoutSeconds, 10, 180);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.TryAddWithoutValidation("x-goog-api-key", settings.ApiKey);
        request.Content = JsonContent.Create(new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = settings.Temperature,
                maxOutputTokens = Math.Clamp(settings.MaxOutputTokens, 1024, 8192)
            }
        }, options: JsonOptions);

        string body;
        try
        {
            using var response = await HttpClient.SendAsync(request, timeoutCts.Token);
            body = await response.Content.ReadAsStringAsync(timeoutCts.Token);

            if (response.IsSuccessStatusCode)
            {
                var text = ExtractText(body);
                if (string.IsNullOrWhiteSpace(text))
                {
                    return ServiceResult<InformeAiGenerationResult>.Validation([
                        new ValidationError(string.Empty, "Gemini no devolvió contenido para el informe.")
                    ]);
                }

                return ServiceResult<InformeAiGenerationResult>.Success(new InformeAiGenerationResult(text.Trim(), settings.Model.Trim()));
            }

            return ServiceResult<InformeAiGenerationResult>.Validation([
                new ValidationError(string.Empty, $"Gemini rechazó la generación del informe: {SanitizeError(body)}")
            ]);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ServiceResult<InformeAiGenerationResult>.Validation([
                new ValidationError(string.Empty, "Gemini no respondió dentro del tiempo configurado.")
            ]);
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult<InformeAiGenerationResult>.Validation([
                new ValidationError(string.Empty, $"No se pudo conectar con Gemini: {ex.Message}")
            ]);
        }
        catch (JsonException ex)
        {
            return ServiceResult<InformeAiGenerationResult>.Validation([
                new ValidationError(string.Empty, $"Gemini devolvió una respuesta no válida: {ex.Message}")
            ]);
        }
    }

    private static Uri BuildEndpoint(GeminiOptions settings)
    {
        var baseUrl = string.IsNullOrWhiteSpace(settings.ApiBaseUrl)
            ? "https://generativelanguage.googleapis.com/v1beta"
            : settings.ApiBaseUrl.TrimEnd('/');
        var model = settings.Model.Trim().Trim('/');
        var modelPath = model.StartsWith("models/", StringComparison.OrdinalIgnoreCase)
            ? model
            : $"models/{model}";

        return new Uri($"{baseUrl}/{modelPath}:generateContent", UriKind.Absolute);
    }

    private static string BuildPrompt(InformeInteligenteContextoDto contexto)
    {
        var payload = contexto with { PromptSugerido = null };
        var json = JsonSerializer.Serialize(payload, JsonOptions);

        return $"""
        Eres un analista senior de gestión para una plataforma SaaS de reservas llamada TuCita.

        Genera un informe ejecutivo profesional en español para el negocio "{contexto.Negocio.Nombre}".

        Usa exclusivamente los datos del JSON que acompaña este prompt. No inventes cifras, no inventes nombres y no agregues supuestos externos.

        Formato requerido:
        # Informe inteligente del negocio
        ## Resumen ejecutivo
        ## Indicadores principales
        ## Hallazgos operativos
        ## Tendencias y riesgos
        ## Recomendaciones priorizadas
        ## Limitaciones de los datos

        Reglas de redacción:
        - Escribe con tono claro, ejecutivo y accionable.
        - Si el período tiene pocos datos, dilo como limitación y evita conclusiones categóricas.
        - Si existe comparación con período anterior, úsala para explicar tendencias.
        - Menciona servicios, horarios, prestadores, cancelaciones, no asistencia, clientes nuevos/recurrentes e ingresos cuando existan datos.
        - Las recomendaciones deben ser concretas y aplicables por Owner/Admin del negocio.
        - Devuelve solo Markdown limpio. No incluyas bloques de código ni el JSON de entrada.

        Datos del negocio y del período:
        {json}
        """;
    }

    private static string ExtractText(string body)
    {
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
            candidates.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var candidate in candidates.EnumerateArray())
        {
            if (!candidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var contentParts) ||
                contentParts.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in contentParts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textElement))
                {
                    var text = textElement.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        parts.Add(text);
                    }
                }
            }
        }

        return string.Join(Environment.NewLine, parts);
    }

    private static string SanitizeError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "sin detalle.";
        }

        var compact = string.Join(' ', body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return compact.Length <= 800 ? compact : compact[..800] + "...";
    }
}
