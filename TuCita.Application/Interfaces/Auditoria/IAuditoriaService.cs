using TuCita.Application.Common;

namespace TuCita.Application.Auditoria;

public interface IAuditoriaService
{
    Task RegistrarAsync(
        CurrentUserContext currentUser,
        AuditoriaRegistro registro,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagedResult<AuditoriaEventoDto>>> GetByNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        AuditoriaQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagedResult<AuditoriaEventoDto>>> GetGlobalAsync(
        CurrentUserContext currentUser,
        AuditoriaQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>> GetCategoriasSelectAsync(
        CurrentUserContext currentUser,
        int? idNegocio,
        AuditoriaFiltroSelectQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>> GetAccionesSelectAsync(
        CurrentUserContext currentUser,
        int? idNegocio,
        AuditoriaFiltroSelectQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>> GetEntidadesSelectAsync(
        CurrentUserContext currentUser,
        int? idNegocio,
        AuditoriaFiltroSelectQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>> GetUsuariosSelectAsync(
        CurrentUserContext currentUser,
        int? idNegocio,
        AuditoriaFiltroSelectQuery query,
        CancellationToken cancellationToken);
}
