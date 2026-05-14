namespace TuCita.Infrastucture.InformesInteligentes;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public bool Enabled { get; init; }

    public string ApiBaseUrl { get; init; } = "https://generativelanguage.googleapis.com/v1beta";

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "gemini-3.1-flash-lite";

    public int TimeoutSeconds { get; init; } = 60;

    public decimal Temperature { get; init; } = 0.35m;

    public int MaxOutputTokens { get; init; } = 4096;
}
