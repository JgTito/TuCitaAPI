namespace TuCita.Application.NegocioUsuarios;

public sealed record NegocioUsuarioDto(
    int IdNegocioUsuario,
    int IdNegocio,
    string Negocio,
    string UserId,
    string? UserName,
    string? Email,
    string? PhoneNumber,
    int IdRolNegocio,
    string RolNegocio,
    bool Activo,
    DateTime FechaCreacion);
