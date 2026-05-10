namespace TuCita.Application.CentroOperativo;

public sealed record CentroOperativoSolicitudCambioDto(
    int IdCita,
    string CodigoCita,
    string Cliente,
    string Servicio,
    string? Prestador,
    DateTime FechaInicio,
    DateTime FechaFin,
    string EstadoCita,
    DateTime? FechaActualizacion,
    string? UltimaObservacion,
    string Prioridad,
    string AccionSugerida);
