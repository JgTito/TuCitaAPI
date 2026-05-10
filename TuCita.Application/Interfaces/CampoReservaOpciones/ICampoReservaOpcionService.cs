using TuCita.Application.Common;

namespace TuCita.Application.CampoReservaOpciones;

public interface ICampoReservaOpcionService
{
    Task<PagedResult<CampoReservaOpcionDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        CampoReservaOpcionQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<CampoReservaOpcionDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        int idCampoReservaOpcion,
        CancellationToken cancellationToken);

    Task<ServiceResult<CampoReservaOpcionDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        CreateCampoReservaOpcionRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CampoReservaOpcionDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        int idCampoReservaOpcion,
        UpdateCampoReservaOpcionRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CampoReservaOpcionDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        int idCampoReservaOpcion,
        bool activo,
        CancellationToken cancellationToken);

    Task<ServiceResult<CampoReservaOpcionDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCampoReserva,
        int idCampoReservaOpcion,
        CancellationToken cancellationToken);
}
