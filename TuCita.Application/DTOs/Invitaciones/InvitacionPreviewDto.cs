namespace TuCita.Application.Invitaciones;

public sealed record InvitacionPreviewDto(
    bool Valida,
    string? Email,
    string? NombreNegocio,
    string? Rol,
    DateTime? FechaExpiracion,
    string? Estado,
    string? Mensaje);
