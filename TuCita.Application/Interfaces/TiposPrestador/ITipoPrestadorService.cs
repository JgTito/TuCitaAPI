using TuCita.Application.Common;

namespace TuCita.Application.TiposPrestador;

public interface ITipoPrestadorService
{
    Task<PagedResult<TipoPrestadorDto>> GetAllAsync(TipoPrestadorQuery query, CancellationToken cancellationToken);

    Task<ServiceResult<TipoPrestadorDto>> GetByIdAsync(int idTipoPrestador, CancellationToken cancellationToken);

    Task<ServiceResult<TipoPrestadorDto>> CreateAsync(CurrentUserContext currentUser, CreateTipoPrestadorRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<TipoPrestadorDto>> UpdateAsync(CurrentUserContext currentUser, int idTipoPrestador, UpdateTipoPrestadorRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<TipoPrestadorDto>> SetActiveAsync(CurrentUserContext currentUser, int idTipoPrestador, bool activo, CancellationToken cancellationToken);

    Task<ServiceResult<TipoPrestadorDto>> DeleteAsync(CurrentUserContext currentUser, int idTipoPrestador, CancellationToken cancellationToken);
}
