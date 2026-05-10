namespace TuCita.Application.Agenda;

public sealed record MiAgendaSlotDisponibleDto(
    int IdNegocio,
    string Negocio,
    DateTime FechaInicio,
    DateTime FechaFin,
    int IdPrestador,
    string Prestador,
    int IdServicio,
    string Servicio);
