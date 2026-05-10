using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Requests.UsuariosPerfil;
using TuCita.Api.Storage;
using TuCita.Application.UsuariosPerfil;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/mi-perfil")]
[Authorize]
public sealed class MiPerfilController(
    IUsuarioPerfilService usuarioPerfilService,
    IFileStorageService fileStorageService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(UsuarioPerfilDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UsuarioPerfilDto>> GetMine(CancellationToken cancellationToken)
    {
        var result = await usuarioPerfilService.GetMineAsync(GetCurrentUser(), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UsuarioPerfilDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UsuarioPerfilDto>> UpdateMine(
        [FromForm] UpdateUsuarioPerfilFormRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var avatarUrl = await SaveAvatarOrAddModelErrorAsync(request.Avatar, cancellationToken);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updateRequest = new UpdateUsuarioPerfilRequest(
            request.Nombre,
            request.Apellido,
            request.Rut,
            request.FechaNacimiento,
            request.TelefonoAlternativo,
            request.Direccion,
            request.IdComuna);

        var result = await usuarioPerfilService.UpdateMineAsync(GetCurrentUser(), updateRequest, avatarUrl, cancellationToken);
        return ToActionResult(result);
    }

    private async Task<string?> SaveAvatarOrAddModelErrorAsync(
        IFormFile? avatar,
        CancellationToken cancellationToken)
    {
        try
        {
            return await fileStorageService.SaveUserAvatarAsync(avatar, cancellationToken);
        }
        catch (FileStorageValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return null;
        }
    }
}
