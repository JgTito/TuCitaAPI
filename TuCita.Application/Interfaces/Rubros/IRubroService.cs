using TuCita.Application.Common;

namespace TuCita.Application.Rubros;

public interface IRubroService
{
    Task<PagedResult<RubroDto>> GetAllAsync(RubroQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RubroSelectDto>> GetSelectAsync(RubroSelectQuery query, CancellationToken cancellationToken);

    Task<ServiceResult<RubroDto>> GetByIdAsync(int idRubro, CancellationToken cancellationToken);

    Task<ServiceResult<RubroDto>> CreateAsync(CurrentUserContext currentUser, CreateRubroRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<RubroDto>> UpdateAsync(CurrentUserContext currentUser, int idRubro, UpdateRubroRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<RubroDto>> SetActiveAsync(CurrentUserContext currentUser, int idRubro, bool activo, CancellationToken cancellationToken);

    Task<ServiceResult<RubroDto>> DeleteAsync(CurrentUserContext currentUser, int idRubro, CancellationToken cancellationToken);
}
