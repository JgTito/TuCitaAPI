using TuCita.Application.Common;

namespace TuCita.Application.Notificaciones;

public interface INotificacionService
{
    Task CrearPorCitaCreadaAsync(
        int idCita,
        CancellationToken cancellationToken);

    Task CrearPorCambioEstadoAsync(
        int idCita,
        string estadoNuevo,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProcesarNotificacionesResultDto>> ProcesarPendientesAsync(
        CurrentUserContext currentUser,
        int? idNegocio,
        int maxNotificaciones,
        CancellationToken cancellationToken);
}
