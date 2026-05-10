namespace TuCita.Application.Agenda;

public sealed record MiAgendaDiaDto(
    DateOnly Fecha,
    AgendaDiaResumenDto Resumen,
    IReadOnlyCollection<MiAgendaCitaDto> Citas,
    IReadOnlyCollection<MiAgendaSlotDisponibleDto> HorasDisponibles,
    IReadOnlyCollection<MiAgendaBloqueoDto> Bloqueos);
