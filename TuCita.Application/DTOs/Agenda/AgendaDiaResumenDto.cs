namespace TuCita.Application.Agenda;

public sealed record AgendaDiaResumenDto(
    int TotalCitas,
    int CitasActivas,
    int CitasFinalizadas,
    int SlotsDisponibles,
    int Bloqueos);
