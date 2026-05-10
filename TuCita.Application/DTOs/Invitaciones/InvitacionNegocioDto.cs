namespace TuCita.Application.Invitaciones;

public sealed record InvitacionNegocioDto(
    int IdInvitacionNegocio,
    int IdNegocio,
    string NombreNegocio,
    int IdRolNegocio,
    string NombreRolNegocio,
    string Email,
    string Estado,
    string InvitadoPorUserId,
    string? AceptadoPorUserId,
    string? CanceladoPorUserId,
    DateTime FechaCreacion,
    DateTime FechaExpiracion,
    DateTime? FechaAceptacion,
    DateTime? FechaCancelacion,
    DateTime? FechaUltimoReenvio,
    string? Mensaje,
    string? MotivoCancelacion);
