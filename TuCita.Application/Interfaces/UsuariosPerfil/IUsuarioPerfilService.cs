using TuCita.Application.Common;

namespace TuCita.Application.UsuariosPerfil;

public interface IUsuarioPerfilService
{
    Task<ServiceResult<UsuarioPerfilDto>> GetMineAsync(
        CurrentUserContext currentUser,
        CancellationToken cancellationToken);

    Task<ServiceResult<UsuarioPerfilDto>> UpdateMineAsync(
        CurrentUserContext currentUser,
        UpdateUsuarioPerfilRequest request,
        string? avatarUrl,
        CancellationToken cancellationToken);
}
