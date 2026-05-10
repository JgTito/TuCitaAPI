namespace TuCita.Application.RolesNegocio;

public sealed record RolNegocioDto(
    int IdRolNegocio,
    string Nombre,
    string? Descripcion,
    bool Activo);
