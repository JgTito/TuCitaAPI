using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Common;
using TuCita.Application.RolesNegocio;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/roles-negocio")]
[Authorize(Roles = "SuperAdmin")]
public sealed class RolesNegocioController(IRolNegocioService rolNegocioService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RolNegocioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RolNegocioDto>>> GetAll(
        [FromQuery] RolNegocioQuery query,
        CancellationToken cancellationToken)
    {
        var roles = await rolNegocioService.GetAllAsync(query, cancellationToken);
        return Ok(roles);
    }

    [HttpGet("{idRolNegocio:int}")]
    [ProducesResponseType(typeof(RolNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RolNegocioDto>> GetById(
        int idRolNegocio,
        CancellationToken cancellationToken)
    {
        var result = await rolNegocioService.GetByIdAsync(idRolNegocio, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RolNegocioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RolNegocioDto>> Create(
        CreateRolNegocioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await rolNegocioService.CreateAsync(GetCurrentUser(), request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetById), new { idRolNegocio = result.Data.IdRolNegocio }, result.Data);
    }

    [HttpPut("{idRolNegocio:int}")]
    [ProducesResponseType(typeof(RolNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RolNegocioDto>> Update(
        int idRolNegocio,
        UpdateRolNegocioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await rolNegocioService.UpdateAsync(GetCurrentUser(), idRolNegocio, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idRolNegocio:int}/activar")]
    [ProducesResponseType(typeof(RolNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RolNegocioDto>> Activate(
        int idRolNegocio,
        CancellationToken cancellationToken)
    {
        var result = await rolNegocioService.SetActiveAsync(GetCurrentUser(), idRolNegocio, activo: true, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idRolNegocio:int}/desactivar")]
    [ProducesResponseType(typeof(RolNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RolNegocioDto>> Deactivate(
        int idRolNegocio,
        CancellationToken cancellationToken)
    {
        var result = await rolNegocioService.SetActiveAsync(GetCurrentUser(), idRolNegocio, activo: false, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{idRolNegocio:int}")]
    [ProducesResponseType(typeof(RolNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RolNegocioDto>> Delete(
        int idRolNegocio,
        CancellationToken cancellationToken)
    {
        var result = await rolNegocioService.DeleteAsync(GetCurrentUser(), idRolNegocio, cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<RolNegocioDto> ToActionResult(ServiceResult<RolNegocioDto> result)
    {
        if (result.Succeeded && result.Data is not null)
        {
            return Ok(result.Data);
        }

        return result.Status switch
        {
            ServiceResultStatus.NotFound => NotFound(result.Errors),
            ServiceResultStatus.Forbidden => Forbid(),
            ServiceResultStatus.Validation => ToValidationProblem<RolNegocioDto>(result.ValidationErrors),
            _ => BadRequest(result.Errors)
        };
    }
}
