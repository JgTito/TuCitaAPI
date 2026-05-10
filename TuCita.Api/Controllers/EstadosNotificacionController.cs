using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Common;
using TuCita.Application.EstadosNotificacion;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/estados-notificacion")]
[Authorize(Roles = "SuperAdmin")]
public sealed class EstadosNotificacionController(IEstadoNotificacionService estadoNotificacionService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EstadoNotificacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EstadoNotificacionDto>>> GetAll(
        [FromQuery] EstadoNotificacionQuery query,
        CancellationToken cancellationToken)
    {
        var estados = await estadoNotificacionService.GetAllAsync(query, cancellationToken);
        return Ok(estados);
    }

    [HttpGet("{idEstadoNotificacion:int}")]
    [ProducesResponseType(typeof(EstadoNotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoNotificacionDto>> GetById(
        int idEstadoNotificacion,
        CancellationToken cancellationToken)
    {
        var result = await estadoNotificacionService.GetByIdAsync(idEstadoNotificacion, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EstadoNotificacionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EstadoNotificacionDto>> Create(
        CreateEstadoNotificacionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await estadoNotificacionService.CreateAsync(GetCurrentUser(), request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetById), new { idEstadoNotificacion = result.Data.IdEstadoNotificacion }, result.Data);
    }

    [HttpPut("{idEstadoNotificacion:int}")]
    [ProducesResponseType(typeof(EstadoNotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoNotificacionDto>> Update(
        int idEstadoNotificacion,
        UpdateEstadoNotificacionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await estadoNotificacionService.UpdateAsync(GetCurrentUser(), idEstadoNotificacion, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idEstadoNotificacion:int}/activar")]
    [ProducesResponseType(typeof(EstadoNotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoNotificacionDto>> Activate(
        int idEstadoNotificacion,
        CancellationToken cancellationToken)
    {
        var result = await estadoNotificacionService.SetActiveAsync(GetCurrentUser(), idEstadoNotificacion, activo: true, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idEstadoNotificacion:int}/desactivar")]
    [ProducesResponseType(typeof(EstadoNotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoNotificacionDto>> Deactivate(
        int idEstadoNotificacion,
        CancellationToken cancellationToken)
    {
        var result = await estadoNotificacionService.SetActiveAsync(GetCurrentUser(), idEstadoNotificacion, activo: false, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{idEstadoNotificacion:int}")]
    [ProducesResponseType(typeof(EstadoNotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoNotificacionDto>> Delete(
        int idEstadoNotificacion,
        CancellationToken cancellationToken)
    {
        var result = await estadoNotificacionService.DeleteAsync(GetCurrentUser(), idEstadoNotificacion, cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<EstadoNotificacionDto> ToActionResult(ServiceResult<EstadoNotificacionDto> result)
    {
        if (result.Succeeded && result.Data is not null)
        {
            return Ok(result.Data);
        }

        return result.Status switch
        {
            ServiceResultStatus.NotFound => NotFound(result.Errors),
            ServiceResultStatus.Forbidden => Forbid(),
            ServiceResultStatus.Validation => ToValidationProblem<EstadoNotificacionDto>(result.ValidationErrors),
            _ => BadRequest(result.Errors)
        };
    }
}
