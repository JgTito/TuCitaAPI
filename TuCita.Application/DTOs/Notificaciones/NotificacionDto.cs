namespace TuCita.Application.Notificaciones;

public sealed record NotificacionDto(
    int IdNotificacion,
    int IdNegocio,
    int? IdCita,
    string TipoNotificacion,
    string CanalNotificacion,
    string EstadoNotificacion,
    string Destinatario,
    string? Asunto,
    string Mensaje,
    DateTime? FechaProgramada,
    DateTime? FechaEnvio,
    string? Error,
    DateTime FechaCreacion);
