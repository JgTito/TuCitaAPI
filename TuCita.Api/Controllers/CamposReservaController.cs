using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.CamposReserva;
using TuCita.Application.Common;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/campos-reserva")]
[Authorize]
public sealed class CamposReservaController(ICampoReservaService campoReservaService) : TuCitaControllerBase
{
    [HttpGet]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(PagedResult<CampoReservaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CampoReservaDto>>> GetAll(
        int idNegocio,
        [FromQuery] CampoReservaQuery query,
        CancellationToken cancellationToken)
    {
        var campos = await campoReservaService.GetAllAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return Ok(campos);
    }

    [HttpGet("select")]
    [Authorize(Policy = TuCitaPolicies.BusinessProfessional)]
    [ProducesResponseType(typeof(IReadOnlyCollection<CampoReservaSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<CampoReservaSelectDto>>> GetSelect(
        int idNegocio,
        [FromQuery] CampoReservaSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var campos = await campoReservaService.GetSelectAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return Ok(campos);
    }

    [HttpGet("{idCampoReserva:int}")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(CampoReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampoReservaDto>> GetById(
        int idNegocio,
        int idCampoReserva,
        CancellationToken cancellationToken)
    {
        var result = await campoReservaService.GetByIdAsync(GetCurrentUser(), idNegocio, idCampoReserva, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(CampoReservaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampoReservaDto>> Create(
        int idNegocio,
        CreateCampoReservaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await campoReservaService.CreateAsync(GetCurrentUser(), idNegocio, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { idNegocio, idCampoReserva = result.Data.IdCampoReserva },
            result.Data);
    }

    [HttpPut("{idCampoReserva:int}")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(CampoReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampoReservaDto>> Update(
        int idNegocio,
        int idCampoReserva,
        UpdateCampoReservaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await campoReservaService.UpdateAsync(GetCurrentUser(), idNegocio, idCampoReserva, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idCampoReserva:int}/activar")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(CampoReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampoReservaDto>> Activate(
        int idNegocio,
        int idCampoReserva,
        CancellationToken cancellationToken)
    {
        var result = await campoReservaService.SetActiveAsync(GetCurrentUser(), idNegocio, idCampoReserva, activo: true, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idCampoReserva:int}/desactivar")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(CampoReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampoReservaDto>> Deactivate(
        int idNegocio,
        int idCampoReserva,
        CancellationToken cancellationToken)
    {
        var result = await campoReservaService.SetActiveAsync(GetCurrentUser(), idNegocio, idCampoReserva, activo: false, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{idCampoReserva:int}")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(CampoReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampoReservaDto>> Delete(
        int idNegocio,
        int idCampoReserva,
        CancellationToken cancellationToken)
    {
        var result = await campoReservaService.DeleteAsync(GetCurrentUser(), idNegocio, idCampoReserva, cancellationToken);
        return ToActionResult(result);
    }
}
