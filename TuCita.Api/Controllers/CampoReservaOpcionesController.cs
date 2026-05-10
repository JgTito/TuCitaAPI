using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.CampoReservaOpciones;
using TuCita.Application.Common;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/campos-reserva/{idCampoReserva:int}/opciones")]
[Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
public sealed class CampoReservaOpcionesController(ICampoReservaOpcionService campoReservaOpcionService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CampoReservaOpcionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CampoReservaOpcionDto>>> GetAll(
        int idNegocio,
        int idCampoReserva,
        [FromQuery] CampoReservaOpcionQuery query,
        CancellationToken cancellationToken)
    {
        var opciones = await campoReservaOpcionService.GetAllAsync(GetCurrentUser(), idNegocio, idCampoReserva, query, cancellationToken);
        return Ok(opciones);
    }

    [HttpGet("{idCampoReservaOpcion:int}")]
    [ProducesResponseType(typeof(CampoReservaOpcionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampoReservaOpcionDto>> GetById(
        int idNegocio,
        int idCampoReserva,
        int idCampoReservaOpcion,
        CancellationToken cancellationToken)
    {
        var result = await campoReservaOpcionService.GetByIdAsync(
            GetCurrentUser(),
            idNegocio,
            idCampoReserva,
            idCampoReservaOpcion,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CampoReservaOpcionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampoReservaOpcionDto>> Create(
        int idNegocio,
        int idCampoReserva,
        CreateCampoReservaOpcionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await campoReservaOpcionService.CreateAsync(GetCurrentUser(), idNegocio, idCampoReserva, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { idNegocio, idCampoReserva, idCampoReservaOpcion = result.Data.IdCampoReservaOpcion },
            result.Data);
    }

    [HttpPut("{idCampoReservaOpcion:int}")]
    [ProducesResponseType(typeof(CampoReservaOpcionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampoReservaOpcionDto>> Update(
        int idNegocio,
        int idCampoReserva,
        int idCampoReservaOpcion,
        UpdateCampoReservaOpcionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await campoReservaOpcionService.UpdateAsync(
            GetCurrentUser(),
            idNegocio,
            idCampoReserva,
            idCampoReservaOpcion,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPatch("{idCampoReservaOpcion:int}/activar")]
    [ProducesResponseType(typeof(CampoReservaOpcionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampoReservaOpcionDto>> Activate(
        int idNegocio,
        int idCampoReserva,
        int idCampoReservaOpcion,
        CancellationToken cancellationToken)
    {
        var result = await campoReservaOpcionService.SetActiveAsync(
            GetCurrentUser(),
            idNegocio,
            idCampoReserva,
            idCampoReservaOpcion,
            activo: true,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPatch("{idCampoReservaOpcion:int}/desactivar")]
    [ProducesResponseType(typeof(CampoReservaOpcionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampoReservaOpcionDto>> Deactivate(
        int idNegocio,
        int idCampoReserva,
        int idCampoReservaOpcion,
        CancellationToken cancellationToken)
    {
        var result = await campoReservaOpcionService.SetActiveAsync(
            GetCurrentUser(),
            idNegocio,
            idCampoReserva,
            idCampoReservaOpcion,
            activo: false,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{idCampoReservaOpcion:int}")]
    [ProducesResponseType(typeof(CampoReservaOpcionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampoReservaOpcionDto>> Delete(
        int idNegocio,
        int idCampoReserva,
        int idCampoReservaOpcion,
        CancellationToken cancellationToken)
    {
        var result = await campoReservaOpcionService.DeleteAsync(GetCurrentUser(), idNegocio, idCampoReserva, idCampoReservaOpcion, cancellationToken);
        return ToActionResult(result);
    }
}
