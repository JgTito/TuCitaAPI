namespace TuCita.Application.Agenda;

public sealed record MovimientosDisponiblesDto(
    int IdCita,
    string Codigo,
    int IdNegocio,
    string Negocio,
    int IdServicio,
    string Servicio,
    int? IdPrestadorActual,
    string? PrestadorActual,
    DateTime FechaInicioActual,
    DateTime FechaFinActual,
    IReadOnlyCollection<MovimientoDisponibleSlotDto> Slots);
