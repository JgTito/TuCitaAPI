using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.Common;
using TuCita.Application.HorariosPrestador;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/prestadores/{idPrestador:int}/horarios")]
[Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
public sealed class HorariosPrestadorController(IHorarioPrestadorService horarioPrestadorService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<HorarioPrestadorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<HorarioPrestadorDto>>> GetAll(
        int idNegocio,
        int idPrestador,
        [FromQuery] HorarioPrestadorQuery query,
        CancellationToken cancellationToken)
    {
        var horarios = await horarioPrestadorService.GetAllAsync(GetCurrentUser(), idNegocio, idPrestador, query, cancellationToken);
        return Ok(horarios);
    }

    [HttpGet("{idHorarioPrestador:int}")]
    [ProducesResponseType(typeof(HorarioPrestadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HorarioPrestadorDto>> GetById(
        int idNegocio,
        int idPrestador,
        int idHorarioPrestador,
        CancellationToken cancellationToken)
    {
        var result = await horarioPrestadorService.GetByIdAsync(
            GetCurrentUser(),
            idNegocio,
            idPrestador,
            idHorarioPrestador,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(HorarioPrestadorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HorarioPrestadorDto>> Create(
        int idNegocio,
        int idPrestador,
        CreateHorarioPrestadorRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await horarioPrestadorService.CreateAsync(GetCurrentUser(), idNegocio, idPrestador, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { idNegocio, idPrestador, idHorarioPrestador = result.Data.IdHorarioPrestador },
            result.Data);
    }

    [HttpPut("{idHorarioPrestador:int}")]
    [ProducesResponseType(typeof(HorarioPrestadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HorarioPrestadorDto>> Update(
        int idNegocio,
        int idPrestador,
        int idHorarioPrestador,
        UpdateHorarioPrestadorRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await horarioPrestadorService.UpdateAsync(
            GetCurrentUser(),
            idNegocio,
            idPrestador,
            idHorarioPrestador,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPatch("{idHorarioPrestador:int}/activar")]
    [ProducesResponseType(typeof(HorarioPrestadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HorarioPrestadorDto>> Activate(
        int idNegocio,
        int idPrestador,
        int idHorarioPrestador,
        CancellationToken cancellationToken)
    {
        var result = await horarioPrestadorService.SetActiveAsync(
            GetCurrentUser(),
            idNegocio,
            idPrestador,
            idHorarioPrestador,
            activo: true,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPatch("{idHorarioPrestador:int}/desactivar")]
    [ProducesResponseType(typeof(HorarioPrestadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HorarioPrestadorDto>> Deactivate(
        int idNegocio,
        int idPrestador,
        int idHorarioPrestador,
        CancellationToken cancellationToken)
    {
        var result = await horarioPrestadorService.SetActiveAsync(
            GetCurrentUser(),
            idNegocio,
            idPrestador,
            idHorarioPrestador,
            activo: false,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{idHorarioPrestador:int}")]
    [ProducesResponseType(typeof(HorarioPrestadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HorarioPrestadorDto>> Delete(
        int idNegocio,
        int idPrestador,
        int idHorarioPrestador,
        CancellationToken cancellationToken)
    {
        var result = await horarioPrestadorService.DeleteAsync(GetCurrentUser(), idNegocio, idPrestador, idHorarioPrestador, cancellationToken);
        return ToActionResult(result);
    }
}
