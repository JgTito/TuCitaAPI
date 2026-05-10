using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.ReglasReserva;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/reglas-reserva")]
[Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
public sealed class ReglasReservaController(IReglaReservaService reglaReservaService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ReglaReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReglaReservaDto>> GetByNegocio(int idNegocio, CancellationToken cancellationToken)
    {
        var result = await reglaReservaService.GetByNegocioAsync(GetCurrentUser(), idNegocio, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReglaReservaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReglaReservaDto>> Create(
        int idNegocio,
        CreateReglaReservaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await reglaReservaService.CreateAsync(GetCurrentUser(), idNegocio, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetByNegocio), new { idNegocio }, result.Data);
    }

    [HttpPut]
    [ProducesResponseType(typeof(ReglaReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReglaReservaDto>> Update(
        int idNegocio,
        UpdateReglaReservaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await reglaReservaService.UpdateAsync(GetCurrentUser(), idNegocio, request, cancellationToken);
        return ToActionResult(result);
    }
}
