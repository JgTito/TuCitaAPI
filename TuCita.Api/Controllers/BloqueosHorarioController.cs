using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.BloqueosHorario;
using TuCita.Application.Common;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/bloqueos-horario")]
[Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
public sealed class BloqueosHorarioController(IBloqueoHorarioService bloqueoHorarioService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BloqueoHorarioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BloqueoHorarioDto>>> GetAll(
        int idNegocio,
        [FromQuery] BloqueoHorarioQuery query,
        CancellationToken cancellationToken)
    {
        var bloqueos = await bloqueoHorarioService.GetAllAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return Ok(bloqueos);
    }

    [HttpGet("{idBloqueoHorario:int}")]
    [ProducesResponseType(typeof(BloqueoHorarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BloqueoHorarioDto>> GetById(
        int idNegocio,
        int idBloqueoHorario,
        CancellationToken cancellationToken)
    {
        var result = await bloqueoHorarioService.GetByIdAsync(GetCurrentUser(), idNegocio, idBloqueoHorario, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BloqueoHorarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BloqueoHorarioDto>> Create(
        int idNegocio,
        CreateBloqueoHorarioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await bloqueoHorarioService.CreateAsync(GetCurrentUser(), idNegocio, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { idNegocio, idBloqueoHorario = result.Data.IdBloqueoHorario },
            result.Data);
    }

    [HttpPut("{idBloqueoHorario:int}")]
    [ProducesResponseType(typeof(BloqueoHorarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BloqueoHorarioDto>> Update(
        int idNegocio,
        int idBloqueoHorario,
        UpdateBloqueoHorarioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await bloqueoHorarioService.UpdateAsync(GetCurrentUser(), idNegocio, idBloqueoHorario, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idBloqueoHorario:int}/activar")]
    [ProducesResponseType(typeof(BloqueoHorarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BloqueoHorarioDto>> Activate(
        int idNegocio,
        int idBloqueoHorario,
        CancellationToken cancellationToken)
    {
        var result = await bloqueoHorarioService.SetActiveAsync(GetCurrentUser(), idNegocio, idBloqueoHorario, activo: true, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idBloqueoHorario:int}/desactivar")]
    [ProducesResponseType(typeof(BloqueoHorarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BloqueoHorarioDto>> Deactivate(
        int idNegocio,
        int idBloqueoHorario,
        CancellationToken cancellationToken)
    {
        var result = await bloqueoHorarioService.SetActiveAsync(GetCurrentUser(), idNegocio, idBloqueoHorario, activo: false, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{idBloqueoHorario:int}")]
    [ProducesResponseType(typeof(BloqueoHorarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BloqueoHorarioDto>> Delete(
        int idNegocio,
        int idBloqueoHorario,
        CancellationToken cancellationToken)
    {
        var result = await bloqueoHorarioService.DeleteAsync(GetCurrentUser(), idNegocio, idBloqueoHorario, cancellationToken);
        return ToActionResult(result);
    }
}
