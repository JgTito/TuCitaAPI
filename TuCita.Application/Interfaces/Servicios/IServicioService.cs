using TuCita.Application.Common;

namespace TuCita.Application.Servicios;

public interface IServicioService
{
    Task<PagedResult<ServicioDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        ServicioQuery query,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ServicioSelectDto>> GetSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        ServicioSelectQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<ServicioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idServicio,
        CancellationToken cancellationToken);

    Task<ServiceResult<ServicioDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateServicioRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ServicioDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idServicio,
        UpdateServicioRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ServicioDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idServicio,
        bool activo,
        CancellationToken cancellationToken);

    Task<ServiceResult<ServicioDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idServicio,
        CancellationToken cancellationToken);
}
