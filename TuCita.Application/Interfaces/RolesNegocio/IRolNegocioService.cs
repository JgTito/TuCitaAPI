using TuCita.Application.Common;

namespace TuCita.Application.RolesNegocio;

public interface IRolNegocioService
{
    Task<PagedResult<RolNegocioDto>> GetAllAsync(RolNegocioQuery query, CancellationToken cancellationToken);

    Task<ServiceResult<RolNegocioDto>> GetByIdAsync(int idRolNegocio, CancellationToken cancellationToken);

    Task<ServiceResult<RolNegocioDto>> CreateAsync(CurrentUserContext currentUser, CreateRolNegocioRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<RolNegocioDto>> UpdateAsync(CurrentUserContext currentUser, int idRolNegocio, UpdateRolNegocioRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<RolNegocioDto>> SetActiveAsync(CurrentUserContext currentUser, int idRolNegocio, bool activo, CancellationToken cancellationToken);

    Task<ServiceResult<RolNegocioDto>> DeleteAsync(CurrentUserContext currentUser, int idRolNegocio, CancellationToken cancellationToken);
}
