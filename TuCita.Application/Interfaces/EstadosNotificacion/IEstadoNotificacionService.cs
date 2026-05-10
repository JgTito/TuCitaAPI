using TuCita.Application.Common;

namespace TuCita.Application.EstadosNotificacion;

public interface IEstadoNotificacionService
{
    Task<PagedResult<EstadoNotificacionDto>> GetAllAsync(EstadoNotificacionQuery query, CancellationToken cancellationToken);

    Task<ServiceResult<EstadoNotificacionDto>> GetByIdAsync(int idEstadoNotificacion, CancellationToken cancellationToken);

    Task<ServiceResult<EstadoNotificacionDto>> CreateAsync(CurrentUserContext currentUser, CreateEstadoNotificacionRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<EstadoNotificacionDto>> UpdateAsync(CurrentUserContext currentUser, int idEstadoNotificacion, UpdateEstadoNotificacionRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<EstadoNotificacionDto>> SetActiveAsync(CurrentUserContext currentUser, int idEstadoNotificacion, bool activo, CancellationToken cancellationToken);

    Task<ServiceResult<EstadoNotificacionDto>> DeleteAsync(CurrentUserContext currentUser, int idEstadoNotificacion, CancellationToken cancellationToken);
}
