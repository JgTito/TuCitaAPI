using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Requests.Negocios;
using TuCita.Api.Storage;
using TuCita.Application.Common;
using TuCita.Application.Negocios;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NegociosController(
    INegocioService negocioService,
    IFileStorageService fileStorageService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NegocioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NegocioDto>>> GetAll(
        [FromQuery] NegocioQuery query,
        CancellationToken cancellationToken)
    {
        var negocios = await negocioService.GetAllAsync(GetCurrentUser(), query, cancellationToken);
        return Ok(negocios);
    }

    [HttpGet("{idNegocio:int}")]
    [ProducesResponseType(typeof(NegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NegocioDto>> GetById(int idNegocio, CancellationToken cancellationToken)
    {
        var result = await negocioService.GetByIdAsync(GetCurrentUser(), idNegocio, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(NegocioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NegocioDto>> Create(
        [FromForm] CreateNegocioFormRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var logoUrl = await SaveLogoOrAddModelErrorAsync(request.Logo, cancellationToken);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var createRequest = new CreateNegocioRequest(
            request.IdRubro,
            request.Nombre,
            request.Slug,
            request.Descripcion,
            logoUrl,
            request.Direccion,
            request.Telefono,
            request.Email,
            request.Activo);

        var result = await negocioService.CreateAsync(GetCurrentUser(), createRequest, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetById), new { idNegocio = result.Data.IdNegocio }, result.Data);
    }

    [HttpPut("{idNegocio:int}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(NegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NegocioDto>> Update(
        int idNegocio,
        [FromForm] UpdateNegocioFormRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var logoUrl = await SaveLogoOrAddModelErrorAsync(request.Logo, cancellationToken);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updateRequest = new UpdateNegocioRequest(
            request.IdRubro,
            request.Nombre,
            request.Slug,
            request.Descripcion,
            logoUrl,
            request.Direccion,
            request.Telefono,
            request.Email,
            request.Activo);

        var result = await negocioService.UpdateAsync(GetCurrentUser(), idNegocio, updateRequest, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idNegocio:int}/activar")]
    [ProducesResponseType(typeof(NegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NegocioDto>> Activate(int idNegocio, CancellationToken cancellationToken)
    {
        var result = await negocioService.SetActiveAsync(GetCurrentUser(), idNegocio, activo: true, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idNegocio:int}/desactivar")]
    [ProducesResponseType(typeof(NegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NegocioDto>> Deactivate(int idNegocio, CancellationToken cancellationToken)
    {
        var result = await negocioService.SetActiveAsync(GetCurrentUser(), idNegocio, activo: false, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{idNegocio:int}")]
    [ProducesResponseType(typeof(NegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NegocioDto>> Delete(int idNegocio, CancellationToken cancellationToken)
    {
        var result = await negocioService.DeleteAsync(GetCurrentUser(), idNegocio, cancellationToken);
        return ToActionResult(result);
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
}
