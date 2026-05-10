namespace TuCita.Application.NegocioUsuarios;

public sealed record NegocioUsuarioSelectDto(
    int IdNegocioUsuario,
    string UserId,
    string Label,
    string? Email,
    string? UserName,
    int IdRolNegocio,
    string RolNegocio,
    bool Activo);
