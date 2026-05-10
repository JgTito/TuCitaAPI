using TuCita.Infrastucture.Entities;

namespace TuCita.Infrastucture.Notificaciones;

public interface INotificacionEmailTemplateRenderer
{
    NotificacionEmailTemplate Render(Notificacion notificacion);
}
