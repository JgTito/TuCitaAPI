using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.Common;
using TuCita.Application.Prestadores;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/prestadores")]
[Authorize]
public sealed class PrestadoresController(IPrestadorService prestadorService) : TuCitaControllerBase
{
    [HttpGet]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(PagedResult<PrestadorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PrestadorDto>>> GetAll(
        int idNegocio,
        [FromQuery] PrestadorQuery query,
        CancellationToken cancellationToken)
    {
        var prestadores = await prestadorService.GetAllAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return Ok(prestadores);
    }

    [HttpGet("select")]
    [Authorize(Policy = TuCitaPolicies.BusinessProfessional)]
    [ProducesResponseType(typeof(IReadOnlyCollection<PrestadorSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<PrestadorSelectDto>>> GetSelect(
        int idNegocio,
        [FromQuery] PrestadorSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var prestadores = await prestadorService.GetSelectAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return Ok(prestadores);
    }

    [HttpGet("{idPrestador:int}")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(PrestadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrestadorDto>> GetById(
        int idNegocio,
        int idPrestador,
        CancellationToken cancellationToken)
    {
        var result = await prestadorService.GetByIdAsync(GetCurrentUser(), idNegocio, idPrestador, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(PrestadorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrestadorDto>> Create(
        int idNegocio,
        CreatePrestadorRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await prestadorService.CreateAsync(GetCurrentUser(), idNegocio, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { idNegocio, idPrestador = result.Data.IdPrestador },
            result.Data);
    }

    [HttpPut("{idPrestador:int}")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(PrestadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrestadorDto>> Update(
        int idNegocio,
        int idPrestador,
        UpdatePrestadorRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await prestadorService.UpdateAsync(GetCurrentUser(), idNegocio, idPrestador, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idPrestador:int}/activar")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(PrestadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrestadorDto>> Activate(
        int idNegocio,
        int idPrestador,
        CancellationToken cancellationToken)
    {
        var result = await prestadorService.SetActiveAsync(
            GetCurrentUser(),
            idNegocio,
            idPrestador,
            activo: true,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPatch("{idPrestador:int}/desactivar")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(PrestadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrestadorDto>> Deactivate(
        int idNegocio,
        int idPrestador,
        CancellationToken cancellationToken)
    {
        var result = await prestadorService.SetActiveAsync(
            GetCurrentUser(),
            idNegocio,
            idPrestador,
            activo: false,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{idPrestador:int}")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(PrestadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrestadorDto>> Delete(
        int idNegocio,
        int idPrestador,
        CancellationToken cancellationToken)
    {
        var result = await prestadorService.DeleteAsync(GetCurrentUser(), idNegocio, idPrestador, cancellationToken);
        return ToActionResult(result);
    }
}
