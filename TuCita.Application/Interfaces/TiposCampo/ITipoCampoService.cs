using TuCita.Application.Common;

namespace TuCita.Application.TiposCampo;

public interface ITipoCampoService
{
    Task<PagedResult<TipoCampoDto>> GetAllAsync(TipoCampoQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TipoCampoSelectDto>> GetSelectAsync(TipoCampoSelectQuery query, CancellationToken cancellationToken);

    Task<ServiceResult<TipoCampoDto>> GetByIdAsync(int idTipoCampo, CancellationToken cancellationToken);

    Task<ServiceResult<TipoCampoDto>> CreateAsync(CurrentUserContext currentUser, CreateTipoCampoRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<TipoCampoDto>> UpdateAsync(CurrentUserContext currentUser, int idTipoCampo, UpdateTipoCampoRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<TipoCampoDto>> SetActiveAsync(CurrentUserContext currentUser, int idTipoCampo, bool activo, CancellationToken cancellationToken);

    Task<ServiceResult<TipoCampoDto>> DeleteAsync(CurrentUserContext currentUser, int idTipoCampo, CancellationToken cancellationToken);
}
