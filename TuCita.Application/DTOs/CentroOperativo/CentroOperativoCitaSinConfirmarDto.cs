namespace TuCita.Application.CentroOperativo;

public sealed record CentroOperativoCitaSinConfirmarDto(
    int IdCita,
    string CodigoCita,
    string Cliente,
    string Servicio,
    string? Prestador,
    DateTime FechaInicio,
    DateTime FechaFin,
    string EstadoCita,
    DateTime FechaCreacion,
    string Prioridad,
    string AccionSugerida);
