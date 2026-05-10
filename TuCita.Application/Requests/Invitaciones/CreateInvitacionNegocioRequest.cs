using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Invitaciones;

public sealed record CreateInvitacionNegocioRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, Range(1, int.MaxValue)] int IdRolNegocio,
    [MaxLength(500)] string? Mensaje,
    DateTime? FechaExpiracion);
