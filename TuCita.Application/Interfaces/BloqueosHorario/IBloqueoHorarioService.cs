using TuCita.Application.Common;

namespace TuCita.Application.BloqueosHorario;

public interface IBloqueoHorarioService
{
    Task<PagedResult<BloqueoHorarioDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        BloqueoHorarioQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<BloqueoHorarioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idBloqueoHorario,
        CancellationToken cancellationToken);

    Task<ServiceResult<BloqueoHorarioDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateBloqueoHorarioRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<BloqueoHorarioDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idBloqueoHorario,
        UpdateBloqueoHorarioRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<BloqueoHorarioDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idBloqueoHorario,
        bool activo,
        CancellationToken cancellationToken);

    Task<ServiceResult<BloqueoHorarioDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idBloqueoHorario,
        CancellationToken cancellationToken);
}
