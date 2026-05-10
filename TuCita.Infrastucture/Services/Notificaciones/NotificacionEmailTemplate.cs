namespace TuCita.Infrastucture.Notificaciones;

public sealed record NotificacionEmailTemplate(
    string Subject,
    string HtmlBody,
    string TextBody);
