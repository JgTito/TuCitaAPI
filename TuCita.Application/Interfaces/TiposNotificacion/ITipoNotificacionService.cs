using TuCita.Application.Common;

namespace TuCita.Application.TiposNotificacion;

public interface ITipoNotificacionService
{
    Task<PagedResult<TipoNotificacionDto>> GetAllAsync(TipoNotificacionQuery query, CancellationToken cancellationToken);

    Task<ServiceResult<TipoNotificacionDto>> GetByIdAsync(int idTipoNotificacion, CancellationToken cancellationToken);

    Task<ServiceResult<TipoNotificacionDto>> CreateAsync(CurrentUserContext currentUser, CreateTipoNotificacionRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<TipoNotificacionDto>> UpdateAsync(CurrentUserContext currentUser, int idTipoNotificacion, UpdateTipoNotificacionRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<TipoNotificacionDto>> SetActiveAsync(CurrentUserContext currentUser, int idTipoNotificacion, bool activo, CancellationToken cancellationToken);

    Task<ServiceResult<TipoNotificacionDto>> DeleteAsync(CurrentUserContext currentUser, int idTipoNotificacion, CancellationToken cancellationToken);
}
