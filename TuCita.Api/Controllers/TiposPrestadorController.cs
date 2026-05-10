using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Common;
using TuCita.Application.TiposPrestador;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/tipos-prestador")]
[Authorize(Roles = "SuperAdmin")]
public sealed class TiposPrestadorController(ITipoPrestadorService tipoPrestadorService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TipoPrestadorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TipoPrestadorDto>>> GetAll(
        [FromQuery] TipoPrestadorQuery query,
        CancellationToken cancellationToken)
    {
        var tipos = await tipoPrestadorService.GetAllAsync(query, cancellationToken);
        return Ok(tipos);
    }

    [HttpGet("{idTipoPrestador:int}")]
    [ProducesResponseType(typeof(TipoPrestadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoPrestadorDto>> GetById(
        int idTipoPrestador,
        CancellationToken cancellationToken)
    {
        var result = await tipoPrestadorService.GetByIdAsync(idTipoPrestador, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TipoPrestadorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TipoPrestadorDto>> Create(
        CreateTipoPrestadorRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await tipoPrestadorService.CreateAsync(GetCurrentUser(), request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetById), new { idTipoPrestador = result.Data.IdTipoPrestador }, result.Data);
    }

    [HttpPut("{idTipoPrestador:int}")]
    [ProducesResponseType(typeof(TipoPrestadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoPrestadorDto>> Update(
        int idTipoPrestador,
        UpdateTipoPrestadorRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await tipoPrestadorService.UpdateAsync(GetCurrentUser(), idTipoPrestador, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idTipoPrestador:int}/activar")]
    [ProducesResponseType(typeof(TipoPrestadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoPrestadorDto>> Activate(
        int idTipoPrestador,
        CancellationToken cancellationToken)
    {
        var result = await tipoPrestadorService.SetActiveAsync(GetCurrentUser(), idTipoPrestador, activo: true, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idTipoPrestador:int}/desactivar")]
    [ProducesResponseType(typeof(TipoPrestadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoPrestadorDto>> Deactivate(
        int idTipoPrestador,
        CancellationToken cancellationToken)
    {
        var result = await tipoPrestadorService.SetActiveAsync(GetCurrentUser(), idTipoPrestador, activo: false, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{idTipoPrestador:int}")]
    [ProducesResponseType(typeof(TipoPrestadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoPrestadorDto>> Delete(
        int idTipoPrestador,
        CancellationToken cancellationToken)
    {
        var result = await tipoPrestadorService.DeleteAsync(GetCurrentUser(), idTipoPrestador, cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<TipoPrestadorDto> ToActionResult(ServiceResult<TipoPrestadorDto> result)
    {
        if (result.Succeeded && result.Data is not null)
        {
            return Ok(result.Data);
        }

        return result.Status switch
        {
            ServiceResultStatus.NotFound => NotFound(result.Errors),
            ServiceResultStatus.Forbidden => Forbid(),
            ServiceResultStatus.Validation => ToValidationProblem<TipoPrestadorDto>(result.ValidationErrors),
            _ => BadRequest(result.Errors)
        };
    }
}
