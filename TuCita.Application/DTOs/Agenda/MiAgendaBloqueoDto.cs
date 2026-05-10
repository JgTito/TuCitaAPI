namespace TuCita.Application.Agenda;

public sealed record MiAgendaBloqueoDto(
    int IdNegocio,
    string Negocio,
    int IdBloqueoHorario,
    int? IdPrestador,
    string? Prestador,
    DateTime FechaInicio,
    DateTime FechaFin,
    string? Motivo);
