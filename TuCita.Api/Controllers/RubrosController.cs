using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Common;
using TuCita.Application.Rubros;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RubrosController(IRubroService rubroService) : TuCitaControllerBase
{
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(PagedResult<RubroDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RubroDto>>> GetAll(
        [FromQuery] RubroQuery query,
        CancellationToken cancellationToken)
    {
        var rubros = await rubroService.GetAllAsync(query, cancellationToken);
        return Ok(rubros);
    }

    [AllowAnonymous]
    [HttpGet("select")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RubroSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<RubroSelectDto>>> GetSelect(
        [FromQuery] RubroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var rubros = await rubroService.GetSelectAsync(query, cancellationToken);
        return Ok(rubros);
    }

    [HttpGet("{idRubro:int}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(RubroDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RubroDto>> GetById(int idRubro, CancellationToken cancellationToken)
    {
        var result = await rubroService.GetByIdAsync(idRubro, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(RubroDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RubroDto>> Create(CreateRubroRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await rubroService.CreateAsync(GetCurrentUser(), request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetById), new { idRubro = result.Data.IdRubro }, result.Data);
    }

    [HttpPut("{idRubro:int}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(RubroDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RubroDto>> Update(
        int idRubro,
        UpdateRubroRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await rubroService.UpdateAsync(GetCurrentUser(), idRubro, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idRubro:int}/activar")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(RubroDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RubroDto>> Activate(int idRubro, CancellationToken cancellationToken)
    {
        var result = await rubroService.SetActiveAsync(GetCurrentUser(), idRubro, activo: true, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idRubro:int}/desactivar")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(RubroDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RubroDto>> Deactivate(int idRubro, CancellationToken cancellationToken)
    {
        var result = await rubroService.SetActiveAsync(GetCurrentUser(), idRubro, activo: false, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{idRubro:int}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(RubroDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RubroDto>> Delete(int idRubro, CancellationToken cancellationToken)
    {
        var result = await rubroService.DeleteAsync(GetCurrentUser(), idRubro, cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<RubroDto> ToActionResult(ServiceResult<RubroDto> result)
    {
        if (result.Succeeded && result.Data is not null)
        {
            return Ok(result.Data);
        }

        return result.Status switch
        {
            ServiceResultStatus.NotFound => NotFound(result.Errors),
            ServiceResultStatus.Forbidden => Forbid(),
            ServiceResultStatus.Validation => ToValidationProblem<RubroDto>(result.ValidationErrors),
            _ => BadRequest(result.Errors)
        };
    }
}
