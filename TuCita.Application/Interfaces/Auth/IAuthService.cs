namespace TuCita.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterClienteAsync(
        RegisterRequest request,
        string? avatarUrl,
        CancellationToken cancellationToken);

    Task<AuthResult> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<AuthResult> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
}
