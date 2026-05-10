using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Citas;
using TuCita.Application.Common;
using TuCita.Application.Pagos;
using TuCita.Application.Resenas;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/mis-citas")]
[Authorize]
public sealed class MisCitasController(
    ICitaService citaService,
    IPagoFlowService pagoFlowService,
    IResenaNegocioService resenaService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CitaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CitaDto>>> GetAll(
        [FromQuery] CitaQuery query,
        CancellationToken cancellationToken)
    {
        var citas = await citaService.GetMisCitasAsync(GetCurrentUser(), query, cancellationToken);
        return Ok(citas);
    }

    [HttpGet("{idCita:int}")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> GetById(
        int idCita,
        CancellationToken cancellationToken)
    {
        var result = await citaService.GetMiCitaByIdAsync(GetCurrentUser(), idCita, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{idCita:int}/historial")]
    [ProducesResponseType(typeof(CitaHistorialTimelineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaHistorialTimelineDto>> GetHistorial(
        int idCita,
        CancellationToken cancellationToken)
    {
        var result = await citaService.GetMiCitaHistorialAsync(GetCurrentUser(), idCita, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{idCita:int}/pagos")]
    [ProducesResponseType(typeof(CitaPagosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaPagosDto>> GetPagos(
        int idCita,
        CancellationToken cancellationToken)
    {
        var result = await pagoFlowService.GetMisPagosCitaAsync(GetCurrentUser(), idCita, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{idCita:int}/resena")]
    [ProducesResponseType(typeof(ResenaNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResenaNegocioDto>> GetResena(
        int idCita,
        CancellationToken cancellationToken)
    {
        var result = await resenaService.GetMiResenaCitaAsync(GetCurrentUser(), idCita, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{idCita:int}/resena")]
    [ProducesResponseType(typeof(ResenaNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResenaNegocioDto>> CrearResena(
        int idCita,
        CrearResenaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await resenaService.CreateMiResenaAsync(GetCurrentUser(), idCita, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{idCita:int}/resena")]
    [ProducesResponseType(typeof(ResenaNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResenaNegocioDto>> ActualizarResena(
        int idCita,
        ActualizarResenaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await resenaService.UpdateMiResenaAsync(GetCurrentUser(), idCita, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{idCita:int}/pagos/{idPago:int}/comprobante")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DescargarComprobantePago(
        int idCita,
        int idPago,
        CancellationToken cancellationToken)
    {
        var result = await pagoFlowService.DescargarComprobanteMiCitaAsync(
            GetCurrentUser(),
            idCita,
            idPago,
            cancellationToken);

        return ToFileResult(result);
    }

    [HttpPatch("{idCita:int}/cancelar")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> Cancelar(
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await citaService.CancelarMiCitaAsync(GetCurrentUser(), idCita, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idCita:int}/reagendar")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> Reagendar(
        int idCita,
        ReagendarCitaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await citaService.ReagendarMiCitaAsync(GetCurrentUser(), idCita, request, cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToFileResult(ServiceResult<PagoComprobanteDto> result)
    {
        if (result.Succeeded && result.Data is not null)
        {
            return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
        }

        return result.Status switch
        {
            ServiceResultStatus.NotFound => NotFound(result.Errors),
            ServiceResultStatus.Forbidden => Forbid(),
            ServiceResultStatus.Validation => ToValidationProblem(result.ValidationErrors),
            _ => BadRequest(result.Errors)
        };
    }

    private IActionResult ToValidationProblem(IReadOnlyCollection<ValidationError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(error.Field, error.Message);
        }

        return ValidationProblem(ModelState);
    }
}
