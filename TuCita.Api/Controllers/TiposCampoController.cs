using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Common;
using TuCita.Application.TiposCampo;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/tipos-campo")]
[Authorize]
public sealed class TiposCampoController(ITipoCampoService tipoCampoService) : TuCitaControllerBase
{
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(PagedResult<TipoCampoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TipoCampoDto>>> GetAll(
        [FromQuery] TipoCampoQuery query,
        CancellationToken cancellationToken)
    {
        var tipos = await tipoCampoService.GetAllAsync(query, cancellationToken);
        return Ok(tipos);
    }

    [HttpGet("select")]
    [Authorize(Roles = "SuperAdmin,Owner,Admin,Recepcionista,Profesional")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TipoCampoSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<TipoCampoSelectDto>>> GetSelect(
        [FromQuery] TipoCampoSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var tipos = await tipoCampoService.GetSelectAsync(query, cancellationToken);
        return Ok(tipos);
    }

    [HttpGet("{idTipoCampo:int}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(TipoCampoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoCampoDto>> GetById(
        int idTipoCampo,
        CancellationToken cancellationToken)
    {
        var result = await tipoCampoService.GetByIdAsync(idTipoCampo, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(TipoCampoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TipoCampoDto>> Create(
        CreateTipoCampoRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await tipoCampoService.CreateAsync(GetCurrentUser(), request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetById), new { idTipoCampo = result.Data.IdTipoCampo }, result.Data);
    }

    [HttpPut("{idTipoCampo:int}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(TipoCampoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoCampoDto>> Update(
        int idTipoCampo,
        UpdateTipoCampoRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await tipoCampoService.UpdateAsync(GetCurrentUser(), idTipoCampo, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idTipoCampo:int}/activar")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(TipoCampoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoCampoDto>> Activate(
        int idTipoCampo,
        CancellationToken cancellationToken)
    {
        var result = await tipoCampoService.SetActiveAsync(GetCurrentUser(), idTipoCampo, activo: true, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idTipoCampo:int}/desactivar")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(TipoCampoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoCampoDto>> Deactivate(
        int idTipoCampo,
        CancellationToken cancellationToken)
    {
        var result = await tipoCampoService.SetActiveAsync(GetCurrentUser(), idTipoCampo, activo: false, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{idTipoCampo:int}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(TipoCampoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoCampoDto>> Delete(
        int idTipoCampo,
        CancellationToken cancellationToken)
    {
        var result = await tipoCampoService.DeleteAsync(GetCurrentUser(), idTipoCampo, cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<TipoCampoDto> ToActionResult(ServiceResult<TipoCampoDto> result)
    {
        if (result.Succeeded && result.Data is not null)
        {
            return Ok(result.Data);
        }

        return result.Status switch
        {
            ServiceResultStatus.NotFound => NotFound(result.Errors),
            ServiceResultStatus.Forbidden => Forbid(),
            ServiceResultStatus.Validation => ToValidationProblem<TipoCampoDto>(result.ValidationErrors),
            _ => BadRequest(result.Errors)
        };
    }
}
