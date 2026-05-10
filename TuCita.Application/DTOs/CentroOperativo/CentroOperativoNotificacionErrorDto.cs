namespace TuCita.Application.CentroOperativo;

public sealed record CentroOperativoNotificacionErrorDto(
    int IdNotificacion,
    int? IdCita,
    string? CodigoCita,
    int? IdResenaNegocio,
    string TipoNotificacion,
    string CanalNotificacion,
    string Destinatario,
    string? Asunto,
    string? Error,
    DateTime? FechaProgramada,
    DateTime FechaCreacion,
    string Prioridad,
    string AccionSugerida);
