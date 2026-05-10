using TuCita.Application.Common;

namespace TuCita.Application.CanalesNotificacion;

public interface ICanalNotificacionService
{
    Task<PagedResult<CanalNotificacionDto>> GetAllAsync(CanalNotificacionQuery query, CancellationToken cancellationToken);

    Task<ServiceResult<CanalNotificacionDto>> GetByIdAsync(int idCanalNotificacion, CancellationToken cancellationToken);

    Task<ServiceResult<CanalNotificacionDto>> CreateAsync(CurrentUserContext currentUser, CreateCanalNotificacionRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<CanalNotificacionDto>> UpdateAsync(CurrentUserContext currentUser, int idCanalNotificacion, UpdateCanalNotificacionRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<CanalNotificacionDto>> SetActiveAsync(CurrentUserContext currentUser, int idCanalNotificacion, bool activo, CancellationToken cancellationToken);

    Task<ServiceResult<CanalNotificacionDto>> DeleteAsync(CurrentUserContext currentUser, int idCanalNotificacion, CancellationToken cancellationToken);
}
