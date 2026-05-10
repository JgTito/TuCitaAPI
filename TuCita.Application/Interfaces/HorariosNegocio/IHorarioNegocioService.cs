using TuCita.Application.Common;

namespace TuCita.Application.HorariosNegocio;

public interface IHorarioNegocioService
{
    Task<PagedResult<HorarioNegocioDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        HorarioNegocioQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<HorarioNegocioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idHorarioNegocio,
        CancellationToken cancellationToken);

    Task<ServiceResult<HorarioNegocioDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateHorarioNegocioRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<HorarioNegocioDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idHorarioNegocio,
        UpdateHorarioNegocioRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<HorarioNegocioDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idHorarioNegocio,
        bool activo,
        CancellationToken cancellationToken);

    Task<ServiceResult<HorarioNegocioDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idHorarioNegocio,
        CancellationToken cancellationToken);
}
