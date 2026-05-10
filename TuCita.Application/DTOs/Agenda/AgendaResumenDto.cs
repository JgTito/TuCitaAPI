namespace TuCita.Application.Agenda;

public sealed record AgendaResumenDto(
    int TotalCitas,
    int CitasActivas,
    int CitasFinalizadas,
    int SlotsDisponibles,
    int Bloqueos,
    decimal IngresosEstimados);
