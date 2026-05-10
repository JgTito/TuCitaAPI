namespace TuCita.Application.Invitaciones;

public sealed record InvitacionCreadaDto(
    InvitacionNegocioDto Invitacion,
    string Token,
    string LinkAceptacion);
