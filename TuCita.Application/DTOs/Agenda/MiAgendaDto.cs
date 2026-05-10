namespace TuCita.Application.Agenda;

public sealed record MiAgendaDto(
    DateOnly FechaDesde,
    DateOnly FechaHasta,
    int IntervaloMinutos,
    int? IdNegocio,
    int? IdServicio,
    int? IdPrestador,
    bool DisponibilidadCalculada,
    AgendaResumenDto Resumen,
    IReadOnlyCollection<MiAgendaPrestadorDto> Prestadores,
    IReadOnlyCollection<MiAgendaDiaDto> Dias,
    IReadOnlyCollection<MiAgendaEventoDto> EventosCalendario);
