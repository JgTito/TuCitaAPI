using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Requests.Onboarding;
using TuCita.Api.Storage;
using TuCita.Application.Onboarding;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/onboarding")]
[AllowAnonymous]
public sealed class OnboardingController(
    IOnboardingService onboardingService,
    IFileStorageService fileStorageService) : TuCitaControllerBase
{
    [HttpPost("negocio")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(OnboardingNegocioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OnboardingNegocioResponse>> RegisterDuenoNegocio(
        [FromForm] RegisterDuenoNegocioFormRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var logoUrl = await SaveLogoOrAddModelErrorAsync(request.Logo, cancellationToken);
        var avatarUrl = await SaveAvatarOrAddModelErrorAsync(request.Avatar, cancellationToken);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var createRequest = new RegisterDuenoNegocioRequest(
            request.EmailUsuario,
            request.Password,
            request.IdRubro,
            request.Nombre,
            request.Slug,
            request.Descripcion,
            logoUrl,
            request.Direccion,
            request.Telefono,
            request.EmailNegocio,
            request.Activo,
            request.NombreUsuario,
            request.ApellidoUsuario,
            request.Rut,
            request.FechaNacimiento,
            request.TelefonoAlternativo,
            request.DireccionUsuario,
            request.IdComuna,
            request.AceptaTerminos,
            request.AceptaMarketing);

        var result = await onboardingService.RegisterDuenoNegocioAsync(createRequest, avatarUrl, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return Created($"/api/negocios/{result.Data.Negocio.IdNegocio}", result.Data);
    }

    private async Task<string?> SaveLogoOrAddModelErrorAsync(
        IFormFile? logo,
        CancellationToken cancellationToken)
    {
        try
        {
            return await fileStorageService.SaveBusinessLogoAsync(logo, cancellationToken);
        }
        catch (FileStorageValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return null;
        }
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
