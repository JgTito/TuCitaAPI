namespace TuCita.Application.CentroOperativo;

public sealed record CentroOperativoResenaBajaDto(
    int IdResenaNegocio,
    int IdCita,
    string CodigoCita,
    string Cliente,
    string Servicio,
    string? Prestador,
    byte Puntuacion,
    string? Comentario,
    string Estado,
    DateTime FechaCreacion,
    DateTime? FechaAlertaOperativa,
    string? MotivoAlertaOperativa,
    string Prioridad,
    string AccionSugerida);
