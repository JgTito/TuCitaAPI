using TuCita.Application.Common;

namespace TuCita.Application.CamposReserva;

public interface ICampoReservaService
{
    Task<PagedResult<CampoReservaDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CampoReservaQuery query,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CampoReservaSelectDto>> GetSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CampoReservaSelectQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<CampoReservaDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        CancellationToken cancellationToken);

    Task<ServiceResult<CampoReservaDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateCampoReservaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CampoReservaDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        UpdateCampoReservaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CampoReservaDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        bool activo,
        CancellationToken cancellationToken);

    Task<ServiceResult<CampoReservaDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        CancellationToken cancellationToken);
}
