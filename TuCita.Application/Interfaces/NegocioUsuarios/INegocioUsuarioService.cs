using TuCita.Application.Common;

namespace TuCita.Application.NegocioUsuarios;

public interface INegocioUsuarioService
{
    Task<PagedResult<NegocioUsuarioDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        NegocioUsuarioQuery query,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<NegocioUsuarioSelectDto>> GetSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        NegocioUsuarioSelectQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<NegocioUsuarioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idNegocioUsuario,
        CancellationToken cancellationToken);

    Task<ServiceResult<NegocioUsuarioDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateNegocioUsuarioRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<NegocioUsuarioDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idNegocioUsuario,
        UpdateNegocioUsuarioRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<NegocioUsuarioDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idNegocioUsuario,
        bool activo,
        CancellationToken cancellationToken);

    Task<ServiceResult<NegocioUsuarioDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idNegocioUsuario,
        CancellationToken cancellationToken);
}
