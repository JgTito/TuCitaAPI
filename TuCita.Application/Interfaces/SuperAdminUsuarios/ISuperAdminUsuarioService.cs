using TuCita.Application.Common;

namespace TuCita.Application.SuperAdminUsuarios;

public interface ISuperAdminUsuarioService
{
    Task<ServiceResult<PagedResult<SuperAdminUsuarioDto>>> GetAllAsync(
        CurrentUserContext currentUser,
        SuperAdminUsuarioQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<SuperAdminUsuarioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        string userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<SuperAdminUsuarioDto>> CreateAsync(
        CurrentUserContext currentUser,
        CreateSuperAdminUsuarioRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<SuperAdminUsuarioDto>> UpdateAsync(
        CurrentUserContext currentUser,
        string userId,
        UpdateSuperAdminUsuarioRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<SuperAdminUsuarioDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        string userId,
        bool activo,
        CancellationToken cancellationToken);

    Task<ServiceResult<SuperAdminUsuarioDto>> UpdateRolesAsync(
        CurrentUserContext currentUser,
        string userId,
        UpdateSuperAdminUsuarioRolesRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<SuperAdminUsuarioDto>> ResetPasswordAsync(
        CurrentUserContext currentUser,
        string userId,
        ResetSuperAdminUsuarioPasswordRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<SuperAdminRolSelectDto>>> GetRolesSelectAsync(
        CurrentUserContext currentUser,
        SuperAdminRolSelectQuery query,
        CancellationToken cancellationToken);
}
