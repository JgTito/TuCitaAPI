using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Requests.Invitaciones;
using TuCita.Api.Storage;
using TuCita.Application.Auth;
using TuCita.Application.Invitaciones;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/invitaciones")]
public sealed class InvitacionesController(
    IInvitacionNegocioService invitacionService,
    IFileStorageService fileStorageService) : TuCitaControllerBase
{
    [HttpPost("validar")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InvitacionPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvitacionPreviewDto>> Validate(
        ValidateInvitacionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await invitacionService.ValidateAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("aceptar")]
    [Authorize]
    [ProducesResponseType(typeof(InvitacionNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InvitacionNegocioDto>> Accept(
        AcceptInvitacionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await invitacionService.AcceptAsync(GetCurrentUser(), request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("registrar-y-aceptar")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> RegisterAndAccept(
        [FromForm] RegisterAndAcceptInvitacionFormRequest request,
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

        var registerRequest = new RegisterAndAcceptInvitacionRequest
        {
            Token = request.Token,
            Password = request.Password,
            ConfirmPassword = request.ConfirmPassword,
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            Rut = request.Rut,
            FechaNacimiento = request.FechaNacimiento,
            TelefonoAlternativo = request.TelefonoAlternativo,
            Direccion = request.Direccion,
            IdComuna = request.IdComuna,
            AceptaTerminos = request.AceptaTerminos,
            AceptaMarketing = request.AceptaMarketing
        };

        var result = await invitacionService.RegisterAndAcceptAsync(registerRequest, avatarUrl, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(RegisterAndAccept), new { userId = result.Data.UserId }, result.Data);
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
