using TuCita.Application.Common;

namespace TuCita.Application.Dashboard;

public interface IDashboardNegocioService
{
    Task<ServiceResult<DashboardNegocioDto>> GetAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        DashboardNegocioQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<DashboardPagosDto>> GetPagosAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        DashboardPagosQuery query,
        CancellationToken cancellationToken);
}
