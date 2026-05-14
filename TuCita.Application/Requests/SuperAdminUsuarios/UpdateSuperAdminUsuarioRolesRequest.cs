namespace TuCita.Application.SuperAdminUsuarios;

public sealed record UpdateSuperAdminUsuarioRolesRequest(
    IReadOnlyCollection<string> Roles);
