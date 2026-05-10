using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class InvitacionNegocio
{
    public int IdInvitacionNegocio { get; set; }
    public int IdNegocio { get; set; }
    public int IdRolNegocio { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string Estado { get; set; } = InvitacionNegocioEstados.Pendiente;
    public string InvitadoPorUserId { get; set; } = string.Empty;
    public string? AceptadoPorUserId { get; set; }
    public string? CanceladoPorUserId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public DateTime? FechaAceptacion { get; set; }
    public DateTime? FechaCancelacion { get; set; }
    public DateTime? FechaUltimoReenvio { get; set; }
    public string? Mensaje { get; set; }
    public string? MotivoCancelacion { get; set; }

    public Negocio Negocio { get; set; } = null!;
    public RolNegocio RolNegocio { get; set; } = null!;
    public IdentityUser InvitadoPor { get; set; } = null!;
    public IdentityUser? AceptadoPor { get; set; }
    public IdentityUser? CanceladoPor { get; set; }
}

public static class InvitacionNegocioEstados
{
    public const string Pendiente = "Pendiente";
    public const string Aceptada = "Aceptada";
    public const string Expirada = "Expirada";
    public const string Cancelada = "Cancelada";
    public const string Reenviada = "Reenviada";
}
