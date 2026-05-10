namespace TuCita.Infrastucture.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; init; } = string.Empty;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public int ExpirationMinutes { get; init; } = 120;

    public int RefreshTokenExpirationDays { get; init; } = 7;
}
