using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class AuthRefreshToken
{
    public int IdRefreshToken { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public DateTime? FechaRevocacion { get; set; }
    public string? ReemplazadoPorTokenHash { get; set; }

    public IdentityUser Usuario { get; set; } = null!;
}
