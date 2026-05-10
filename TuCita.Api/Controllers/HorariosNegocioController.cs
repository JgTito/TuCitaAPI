using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.Common;
using TuCita.Application.HorariosNegocio;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/horarios")]
[Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
public sealed class HorariosNegocioController(IHorarioNegocioService horarioNegocioService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<HorarioNegocioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<HorarioNegocioDto>>> GetAll(
        int idNegocio,
        [FromQuery] HorarioNegocioQuery query,
        CancellationToken cancellationToken)
    {
        var horarios = await horarioNegocioService.GetAllAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return Ok(horarios);
    }

    [HttpGet("{idHorarioNegocio:int}")]
    [ProducesResponseType(typeof(HorarioNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HorarioNegocioDto>> GetById(
        int idNegocio,
        int idHorarioNegocio,
        CancellationToken cancellationToken)
    {
        var result = await horarioNegocioService.GetByIdAsync(GetCurrentUser(), idNegocio, idHorarioNegocio, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(HorarioNegocioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HorarioNegocioDto>> Create(
        int idNegocio,
        CreateHorarioNegocioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await horarioNegocioService.CreateAsync(GetCurrentUser(), idNegocio, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { idNegocio, idHorarioNegocio = result.Data.IdHorarioNegocio },
            result.Data);
    }

    [HttpPut("{idHorarioNegocio:int}")]
    [ProducesResponseType(typeof(HorarioNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HorarioNegocioDto>> Update(
        int idNegocio,
        int idHorarioNegocio,
        UpdateHorarioNegocioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await horarioNegocioService.UpdateAsync(GetCurrentUser(), idNegocio, idHorarioNegocio, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idHorarioNegocio:int}/activar")]
    [ProducesResponseType(typeof(HorarioNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HorarioNegocioDto>> Activate(
        int idNegocio,
        int idHorarioNegocio,
        CancellationToken cancellationToken)
    {
        var result = await horarioNegocioService.SetActiveAsync(
            GetCurrentUser(),
            idNegocio,
            idHorarioNegocio,
            activo: true,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPatch("{idHorarioNegocio:int}/desactivar")]
    [ProducesResponseType(typeof(HorarioNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HorarioNegocioDto>> Deactivate(
        int idNegocio,
        int idHorarioNegocio,
        CancellationToken cancellationToken)
    {
        var result = await horarioNegocioService.SetActiveAsync(
            GetCurrentUser(),
            idNegocio,
            idHorarioNegocio,
            activo: false,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{idHorarioNegocio:int}")]
    [ProducesResponseType(typeof(HorarioNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HorarioNegocioDto>> Delete(
        int idNegocio,
        int idHorarioNegocio,
        CancellationToken cancellationToken)
    {
        var result = await horarioNegocioService.DeleteAsync(GetCurrentUser(), idNegocio, idHorarioNegocio, cancellationToken);
        return ToActionResult(result);
    }
}
