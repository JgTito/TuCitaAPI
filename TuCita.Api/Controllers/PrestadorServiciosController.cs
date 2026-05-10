using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.Common;
using TuCita.Application.PrestadorServicios;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/prestadores/{idPrestador:int}/servicios")]
[Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
public sealed class PrestadorServiciosController(IPrestadorServicioService prestadorServicioService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PrestadorServicioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PrestadorServicioDto>>> GetAll(
        int idNegocio,
        int idPrestador,
        [FromQuery] PrestadorServicioQuery query,
        CancellationToken cancellationToken)
    {
        var relaciones = await prestadorServicioService.GetAllAsync(GetCurrentUser(), idNegocio, idPrestador, query, cancellationToken);
        return Ok(relaciones);
    }

    [HttpGet("{idPrestadorServicio:int}")]
    [ProducesResponseType(typeof(PrestadorServicioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrestadorServicioDto>> GetById(
        int idNegocio,
        int idPrestador,
        int idPrestadorServicio,
        CancellationToken cancellationToken)
    {
        var result = await prestadorServicioService.GetByIdAsync(
            GetCurrentUser(),
            idNegocio,
            idPrestador,
            idPrestadorServicio,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PrestadorServicioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrestadorServicioDto>> Create(
        int idNegocio,
        int idPrestador,
        CreatePrestadorServicioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await prestadorServicioService.CreateAsync(GetCurrentUser(), idNegocio, idPrestador, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                idNegocio,
                idPrestador,
                idPrestadorServicio = result.Data.IdPrestadorServicio
            },
            result.Data);
    }

    [HttpPut("{idPrestadorServicio:int}")]
    [ProducesResponseType(typeof(PrestadorServicioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrestadorServicioDto>> Update(
        int idNegocio,
        int idPrestador,
        int idPrestadorServicio,
        UpdatePrestadorServicioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await prestadorServicioService.UpdateAsync(
            GetCurrentUser(),
            idNegocio,
            idPrestador,
            idPrestadorServicio,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPatch("{idPrestadorServicio:int}/activar")]
    [ProducesResponseType(typeof(PrestadorServicioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrestadorServicioDto>> Activate(
        int idNegocio,
        int idPrestador,
        int idPrestadorServicio,
        CancellationToken cancellationToken)
    {
        var result = await prestadorServicioService.SetActiveAsync(
            GetCurrentUser(),
            idNegocio,
            idPrestador,
            idPrestadorServicio,
            activo: true,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPatch("{idPrestadorServicio:int}/desactivar")]
    [ProducesResponseType(typeof(PrestadorServicioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrestadorServicioDto>> Deactivate(
        int idNegocio,
        int idPrestador,
        int idPrestadorServicio,
        CancellationToken cancellationToken)
    {
        var result = await prestadorServicioService.SetActiveAsync(
            GetCurrentUser(),
            idNegocio,
            idPrestador,
            idPrestadorServicio,
            activo: false,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{idPrestadorServicio:int}")]
    [ProducesResponseType(typeof(PrestadorServicioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrestadorServicioDto>> Delete(
        int idNegocio,
        int idPrestador,
        int idPrestadorServicio,
        CancellationToken cancellationToken)
    {
        var result = await prestadorServicioService.DeleteAsync(
            GetCurrentUser(),
            idNegocio,
            idPrestador,
            idPrestadorServicio,
            cancellationToken);

        return ToActionResult(result);
    }
}
