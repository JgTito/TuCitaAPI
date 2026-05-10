using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.CanalesNotificacion;
using TuCita.Application.Common;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/canales-notificacion")]
[Authorize(Roles = "SuperAdmin")]
public sealed class CanalesNotificacionController(ICanalNotificacionService canalNotificacionService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CanalNotificacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CanalNotificacionDto>>> GetAll(
        [FromQuery] CanalNotificacionQuery query,
        CancellationToken cancellationToken)
    {
        var canales = await canalNotificacionService.GetAllAsync(query, cancellationToken);
        return Ok(canales);
    }

    [HttpGet("{idCanalNotificacion:int}")]
    [ProducesResponseType(typeof(CanalNotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CanalNotificacionDto>> GetById(
        int idCanalNotificacion,
        CancellationToken cancellationToken)
    {
        var result = await canalNotificacionService.GetByIdAsync(idCanalNotificacion, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CanalNotificacionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CanalNotificacionDto>> Create(
        CreateCanalNotificacionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await canalNotificacionService.CreateAsync(GetCurrentUser(), request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetById), new { idCanalNotificacion = result.Data.IdCanalNotificacion }, result.Data);
    }

    [HttpPut("{idCanalNotificacion:int}")]
    [ProducesResponseType(typeof(CanalNotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CanalNotificacionDto>> Update(
        int idCanalNotificacion,
        UpdateCanalNotificacionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await canalNotificacionService.UpdateAsync(GetCurrentUser(), idCanalNotificacion, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idCanalNotificacion:int}/activar")]
    [ProducesResponseType(typeof(CanalNotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CanalNotificacionDto>> Activate(
        int idCanalNotificacion,
        CancellationToken cancellationToken)
    {
        var result = await canalNotificacionService.SetActiveAsync(GetCurrentUser(), idCanalNotificacion, activo: true, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idCanalNotificacion:int}/desactivar")]
    [ProducesResponseType(typeof(CanalNotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CanalNotificacionDto>> Deactivate(
        int idCanalNotificacion,
        CancellationToken cancellationToken)
    {
        var result = await canalNotificacionService.SetActiveAsync(GetCurrentUser(), idCanalNotificacion, activo: false, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{idCanalNotificacion:int}")]
    [ProducesResponseType(typeof(CanalNotificacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CanalNotificacionDto>> Delete(
        int idCanalNotificacion,
        CancellationToken cancellationToken)
    {
        var result = await canalNotificacionService.DeleteAsync(GetCurrentUser(), idCanalNotificacion, cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<CanalNotificacionDto> ToActionResult(ServiceResult<CanalNotificacionDto> result)
    {
        if (result.Succeeded && result.Data is not null)
        {
            return Ok(result.Data);
        }

        return result.Status switch
        {
            ServiceResultStatus.NotFound => NotFound(result.Errors),
            ServiceResultStatus.Forbidden => Forbid(),
            ServiceResultStatus.Validation => ToValidationProblem<CanalNotificacionDto>(result.ValidationErrors),
            _ => BadRequest(result.Errors)
        };
    }
}
