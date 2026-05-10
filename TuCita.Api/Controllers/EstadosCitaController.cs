using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Common;
using TuCita.Application.EstadosCita;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/estados-cita")]
[Authorize]
public sealed class EstadosCitaController(IEstadoCitaService estadoCitaService) : TuCitaControllerBase
{
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(PagedResult<EstadoCitaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EstadoCitaDto>>> GetAll(
        [FromQuery] EstadoCitaQuery query,
        CancellationToken cancellationToken)
    {
        var estados = await estadoCitaService.GetAllAsync(query, cancellationToken);
        return Ok(estados);
    }

    [HttpGet("select")]
    [ProducesResponseType(typeof(IReadOnlyCollection<EstadoCitaSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<EstadoCitaSelectDto>>> GetSelect(
        [FromQuery] EstadoCitaSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var estados = await estadoCitaService.GetSelectAsync(query, cancellationToken);
        return Ok(estados);
    }

    [HttpGet("{idEstadoCita:int}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(EstadoCitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoCitaDto>> GetById(
        int idEstadoCita,
        CancellationToken cancellationToken)
    {
        var result = await estadoCitaService.GetByIdAsync(idEstadoCita, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(EstadoCitaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EstadoCitaDto>> Create(
        CreateEstadoCitaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await estadoCitaService.CreateAsync(GetCurrentUser(), request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetById), new { idEstadoCita = result.Data.IdEstadoCita }, result.Data);
    }

    [HttpPut("{idEstadoCita:int}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(EstadoCitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoCitaDto>> Update(
        int idEstadoCita,
        UpdateEstadoCitaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await estadoCitaService.UpdateAsync(GetCurrentUser(), idEstadoCita, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idEstadoCita:int}/activar")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(EstadoCitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoCitaDto>> Activate(
        int idEstadoCita,
        CancellationToken cancellationToken)
    {
        var result = await estadoCitaService.SetActiveAsync(GetCurrentUser(), idEstadoCita, activo: true, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idEstadoCita:int}/desactivar")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(EstadoCitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoCitaDto>> Deactivate(
        int idEstadoCita,
        CancellationToken cancellationToken)
    {
        var result = await estadoCitaService.SetActiveAsync(GetCurrentUser(), idEstadoCita, activo: false, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{idEstadoCita:int}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(EstadoCitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoCitaDto>> Delete(
        int idEstadoCita,
        CancellationToken cancellationToken)
    {
        var result = await estadoCitaService.DeleteAsync(GetCurrentUser(), idEstadoCita, cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<EstadoCitaDto> ToActionResult(ServiceResult<EstadoCitaDto> result)
    {
        if (result.Succeeded && result.Data is not null)
        {
            return Ok(result.Data);
        }

        return result.Status switch
        {
            ServiceResultStatus.NotFound => NotFound(result.Errors),
            ServiceResultStatus.Forbidden => Forbid(),
            ServiceResultStatus.Validation => ToValidationProblem<EstadoCitaDto>(result.ValidationErrors),
            _ => BadRequest(result.Errors)
        };
    }
}
