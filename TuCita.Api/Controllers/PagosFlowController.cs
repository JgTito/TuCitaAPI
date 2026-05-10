using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Requests.Pagos;
using TuCita.Application.Pagos;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/pagos/flow")]
public sealed class PagosFlowController(IPagoFlowService pagoFlowService) : TuCitaControllerBase
{
    [HttpPost("confirmacion")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(typeof(PagoFlowResultadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagoFlowResultadoDto>> Confirmacion(
        [FromForm] FlowTokenFormRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await pagoFlowService.ConfirmarPagoAsync(request.Token, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("retorno")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(PagoFlowResultadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagoFlowResultadoDto>> Retorno(
        [FromForm] FlowTokenFormRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await pagoFlowService.ProcesarRetornoAsync(request.Token, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return string.IsNullOrWhiteSpace(result.Data.FrontendRedirectUrl)
            ? Ok(result.Data)
            : Redirect(result.Data.FrontendRedirectUrl);
    }

    [HttpGet("resultado/{commerceOrder}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagoFlowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagoFlowDto>> GetResultado(
        string commerceOrder,
        CancellationToken cancellationToken)
    {
        var result = await pagoFlowService.GetByCommerceOrderAsync(commerceOrder, cancellationToken);
        return ToActionResult(result);
    }
}
