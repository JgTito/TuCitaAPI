using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.Pagos;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/pagos/manuales")]
[Authorize(Policy = TuCitaPolicies.BusinessAgenda)]
public sealed class PagosManualesController(IPagoFlowService pagoFlowService) : TuCitaControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(PagoNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagoNegocioDto>> Registrar(
        int idNegocio,
        [FromBody] RegistrarPagoManualRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await pagoFlowService.RegistrarPagoManualAsync(
            GetCurrentUser(),
            idNegocio,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{idPago:int}/anular")]
    [ProducesResponseType(typeof(PagoNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagoNegocioDto>> Anular(
        int idNegocio,
        int idPago,
        [FromBody] AnularPagoManualRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await pagoFlowService.AnularPagoManualAsync(
            GetCurrentUser(),
            idNegocio,
            idPago,
            request,
            cancellationToken);

        return ToActionResult(result);
    }
}
