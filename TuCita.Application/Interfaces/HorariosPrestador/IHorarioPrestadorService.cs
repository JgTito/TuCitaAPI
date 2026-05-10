using TuCita.Application.Common;

namespace TuCita.Application.HorariosPrestador;

public interface IHorarioPrestadorService
{
    Task<PagedResult<HorarioPrestadorDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        HorarioPrestadorQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<HorarioPrestadorDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        int idHorarioPrestador,
        CancellationToken cancellationToken);

    Task<ServiceResult<HorarioPrestadorDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        CreateHorarioPrestadorRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<HorarioPrestadorDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        int idHorarioPrestador,
        UpdateHorarioPrestadorRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<HorarioPrestadorDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        int idHorarioPrestador,
        bool activo,
        CancellationToken cancellationToken);

    Task<ServiceResult<HorarioPrestadorDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        int idHorarioPrestador,
        CancellationToken cancellationToken);
}
