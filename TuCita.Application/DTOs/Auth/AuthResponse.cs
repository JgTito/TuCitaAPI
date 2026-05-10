namespace TuCita.Application.Auth;

public sealed record AuthResponse(
    string UserId,
    string Email,
    string UserName,
    IReadOnlyCollection<string> Roles,
    string Token,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    string? AvatarUrl);
