using TuCita.Application.Common;

namespace TuCita.Application.ReglasReserva;

public interface IReglaReservaService
{
    Task<ServiceResult<ReglaReservaDto>> GetByNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken);

    Task<ServiceResult<ReglaReservaDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateReglaReservaRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ReglaReservaDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        UpdateReglaReservaRequest request,
        CancellationToken cancellationToken);
}
