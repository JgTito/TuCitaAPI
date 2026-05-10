namespace TuCita.Application.Notificaciones;

public sealed record ProcesarNotificacionesResultDto(
    int TotalProcesadas,
    int TotalEnviadas,
    int TotalConError,
    IReadOnlyCollection<NotificacionDto> Notificaciones);
