namespace TuCita.Application.SuperAdminUsuarios;

public sealed record SuperAdminUsuarioNegocioDto(
    int IdNegocioUsuario,
    int IdNegocio,
    string Negocio,
    int IdRolNegocio,
    string RolNegocio,
    bool Activo,
    DateTime FechaCreacion);
