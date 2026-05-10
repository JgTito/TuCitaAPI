using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Common;
using TuCita.Application.TiposNotificacion;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/tipos-notificacion")]
[Authorize(Roles = "SuperAdmin")]
public sealed class TiposNotificacionController(ITipoNotificacionService tipoNotificacionService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TipoNotificacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TipoNotificacionDto>>> GetAll(
        [FromQuery] TipoNotificacionQuery query,
        CancellationToken cancellationToken)
    {
        var tipos = await tipoNotificacionService.GetAllAsync(query, cancellationToken);
        return Ok(tipos);
    }

    [HttpGet("{idTipoNotificacion:int}")]
    [ProducesResponseType(typeof(TipoNotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoNotificacionDto>> GetById(
        int idTipoNotificacion,
        CancellationToken cancellationToken)
    {
        var result = await tipoNotificacionService.GetByIdAsync(idTipoNotificacion, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TipoNotificacionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TipoNotificacionDto>> Create(
        CreateTipoNotificacionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await tipoNotificacionService.CreateAsync(GetCurrentUser(), request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetById), new { idTipoNotificacion = result.Data.IdTipoNotificacion }, result.Data);
    }

    [HttpPut("{idTipoNotificacion:int}")]
    [ProducesResponseType(typeof(TipoNotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoNotificacionDto>> Update(
        int idTipoNotificacion,
        UpdateTipoNotificacionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await tipoNotificacionService.UpdateAsync(GetCurrentUser(), idTipoNotificacion, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idTipoNotificacion:int}/activar")]
    [ProducesResponseType(typeof(TipoNotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoNotificacionDto>> Activate(
        int idTipoNotificacion,
        CancellationToken cancellationToken)
    {
        var result = await tipoNotificacionService.SetActiveAsync(GetCurrentUser(), idTipoNotificacion, activo: true, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idTipoNotificacion:int}/desactivar")]
    [ProducesResponseType(typeof(TipoNotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoNotificacionDto>> Deactivate(
        int idTipoNotificacion,
        CancellationToken cancellationToken)
    {
        var result = await tipoNotificacionService.SetActiveAsync(GetCurrentUser(), idTipoNotificacion, activo: false, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{idTipoNotificacion:int}")]
    [ProducesResponseType(typeof(TipoNotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoNotificacionDto>> Delete(
        int idTipoNotificacion,
        CancellationToken cancellationToken)
    {
        var result = await tipoNotificacionService.DeleteAsync(GetCurrentUser(), idTipoNotificacion, cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<TipoNotificacionDto> ToActionResult(ServiceResult<TipoNotificacionDto> result)
    {
        if (result.Succeeded && result.Data is not null)
        {
            return Ok(result.Data);
        }

        return result.Status switch
        {
            ServiceResultStatus.NotFound => NotFound(result.Errors),
            ServiceResultStatus.Forbidden => Forbid(),
            ServiceResultStatus.Validation => ToValidationProblem<TipoNotificacionDto>(result.ValidationErrors),
            _ => BadRequest(result.Errors)
        };
    }
}
