namespace TuCita.Application.Agenda;

public sealed record MovimientoDisponibleSlotDto(
    DateTime FechaInicio,
    DateTime FechaFin,
    int IdPrestador,
    string Prestador);
