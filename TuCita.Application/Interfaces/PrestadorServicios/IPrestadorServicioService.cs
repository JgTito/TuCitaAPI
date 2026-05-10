using TuCita.Application.Common;

namespace TuCita.Application.PrestadorServicios;

public interface IPrestadorServicioService
{
    Task<PagedResult<PrestadorServicioDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        PrestadorServicioQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<PrestadorServicioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        int idPrestadorServicio,
        CancellationToken cancellationToken);

    Task<ServiceResult<PrestadorServicioDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        CreatePrestadorServicioRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<PrestadorServicioDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        int idPrestadorServicio,
        UpdatePrestadorServicioRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<PrestadorServicioDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        int idPrestadorServicio,
        bool activo,
        CancellationToken cancellationToken);

    Task<ServiceResult<PrestadorServicioDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        int idPrestadorServicio,
        CancellationToken cancellationToken);
}
