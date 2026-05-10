using TuCita.Application.Common;

namespace TuCita.Application.Negocios;

public interface INegocioService
{
    Task<PagedResult<NegocioDto>> GetAllAsync(CurrentUserContext currentUser, NegocioQuery query, CancellationToken cancellationToken);

    Task<ServiceResult<NegocioDto>> GetByIdAsync(CurrentUserContext currentUser, int idNegocio, CancellationToken cancellationToken);

    Task<ServiceResult<NegocioDto>> CreateAsync(CurrentUserContext currentUser, CreateNegocioRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<NegocioDto>> UpdateAsync(CurrentUserContext currentUser, int idNegocio, UpdateNegocioRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<NegocioDto>> SetActiveAsync(CurrentUserContext currentUser, int idNegocio, bool activo, CancellationToken cancellationToken);

    Task<ServiceResult<NegocioDto>> DeleteAsync(CurrentUserContext currentUser, int idNegocio, CancellationToken cancellationToken);
}
