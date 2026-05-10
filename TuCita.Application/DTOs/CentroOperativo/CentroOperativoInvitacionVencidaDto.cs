namespace TuCita.Application.CentroOperativo;

public sealed record CentroOperativoInvitacionVencidaDto(
    int IdInvitacionNegocio,
    string Email,
    int IdRolNegocio,
    string RolNegocio,
    string Estado,
    DateTime FechaCreacion,
    DateTime FechaExpiracion,
    int DiasVencida,
    string Prioridad,
    string AccionSugerida);
