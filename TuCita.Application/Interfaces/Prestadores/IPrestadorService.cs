using TuCita.Application.Common;

namespace TuCita.Application.Prestadores;

public interface IPrestadorService
{
    Task<PagedResult<PrestadorDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PrestadorQuery query,
        CancellationToken cancellationToken);

    Task<PagedResult<PrestadorDto>> GetFromAssociatedBusinessesAsync(
        CurrentUserContext currentUser,
        PrestadorQuery query,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PrestadorSelectDto>> GetSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PrestadorSelectQuery query,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PrestadorSelectDto>> GetSelectFromAssociatedBusinessesAsync(
        CurrentUserContext currentUser,
        PrestadorSelectQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<PrestadorDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        CancellationToken cancellationToken);

    Task<ServiceResult<PrestadorDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreatePrestadorRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<PrestadorDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        UpdatePrestadorRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<PrestadorDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        bool activo,
        CancellationToken cancellationToken);

    Task<ServiceResult<PrestadorDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        CancellationToken cancellationToken);
}
