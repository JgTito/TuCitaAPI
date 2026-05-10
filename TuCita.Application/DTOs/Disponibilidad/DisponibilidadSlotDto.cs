namespace TuCita.Application.Disponibilidad;

public sealed record DisponibilidadSlotDto(
    DateTime FechaInicio,
    DateTime FechaFin,
    int? IdPrestador,
    string? Prestador);
