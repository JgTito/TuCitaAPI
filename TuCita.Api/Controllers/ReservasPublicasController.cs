using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Common;
using TuCita.Application.Disponibilidad;
using TuCita.Application.Pagos;
using TuCita.Application.ReservasPublicas;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/reservas-publicas/{slug}")]
[AllowAnonymous]
public sealed class ReservasPublicasController(
    IReservaPublicaService reservaPublicaService,
    IPagoFlowService pagoFlowService) : TuCitaControllerBase
{
    [HttpGet("negocio")]
    [ProducesResponseType(typeof(PublicNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicNegocioDto>> GetNegocio(
        string slug,
        CancellationToken cancellationToken)
    {
        var result = await reservaPublicaService.GetNegocioAsync(slug, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("servicios")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PublicServicioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<PublicServicioDto>>> GetServicios(
        string slug,
        CancellationToken cancellationToken)
    {
        var result = await reservaPublicaService.GetServiciosAsync(slug, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("servicios/{idServicio:int}/prestadores")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PublicPrestadorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<PublicPrestadorDto>>> GetPrestadores(
        string slug,
        int idServicio,
        CancellationToken cancellationToken)
    {
        var result = await reservaPublicaService.GetPrestadoresAsync(slug, idServicio, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("disponibilidad")]
    [ProducesResponseType(typeof(DisponibilidadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DisponibilidadDto>> GetDisponibilidad(
        string slug,
        [FromQuery] DisponibilidadQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await reservaPublicaService.GetDisponibilidadAsync(slug, query, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("campos")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PublicCampoReservaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<PublicCampoReservaDto>>> GetCampos(
        string slug,
        CancellationToken cancellationToken)
    {
        var result = await reservaPublicaService.GetCamposReservaAsync(slug, idServicio: null, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("servicios/{idServicio:int}/campos")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PublicCampoReservaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<PublicCampoReservaDto>>> GetCamposPorServicio(
        string slug,
        int idServicio,
        CancellationToken cancellationToken)
    {
        var result = await reservaPublicaService.GetCamposReservaAsync(slug, idServicio, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("reglas")]
    [ProducesResponseType(typeof(PublicReglaReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicReglaReservaDto>> GetReglasReserva(
        string slug,
        CancellationToken cancellationToken)
    {
        var result = await reservaPublicaService.GetReglasReservaAsync(slug, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("reservas/{codigo}")]
    [ProducesResponseType(typeof(PublicReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicReservaDto>> GetReservaByCodigo(
        string slug,
        string codigo,
        CancellationToken cancellationToken)
    {
        var result = await reservaPublicaService.GetReservaByCodigoAsync(slug, codigo, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("reservas/{codigo}/pago-flow")]
    [ProducesResponseType(typeof(CrearPagoFlowResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CrearPagoFlowResponseDto>> CrearPagoFlow(
        string slug,
        string codigo,
        CrearPagoReservaPublicaRequest? request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await pagoFlowService.CrearPagoReservaPublicaAsync(
            GetCurrentUser(),
            slug,
            codigo,
            request ?? new CrearPagoReservaPublicaRequest(null, null),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("reservas/{codigo}/pagos/{idPago:int}/comprobante")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DescargarComprobantePago(
        string slug,
        string codigo,
        int idPago,
        [FromQuery] DescargarComprobantePublicoRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await pagoFlowService.DescargarComprobanteReservaPublicaAsync(
            GetCurrentUser(),
            slug,
            codigo,
            idPago,
            request,
            cancellationToken);

        return ToFileResult(result);
    }

    [HttpPatch("reservas/{codigo}/cancelar")]
    [ProducesResponseType(typeof(PublicReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicReservaDto>> CancelReserva(
        string slug,
        string codigo,
        CancelReservaPublicaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Telefono))
        {
            ModelState.AddModelError(string.Empty, "Debes indicar el email o teléfono usado en la reserva.");
            return ValidationProblem(ModelState);
        }

        var result = await reservaPublicaService.CancelReservaAsync(slug, codigo, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("reservas/{codigo}/reagendar")]
    [ProducesResponseType(typeof(PublicReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicReservaDto>> ReagendarReserva(
        string slug,
        string codigo,
        ReagendarReservaPublicaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Telefono))
        {
            ModelState.AddModelError(string.Empty, "Debes indicar el email o teléfono usado en la reserva.");
            return ValidationProblem(ModelState);
        }

        var result = await reservaPublicaService.ReagendarReservaAsync(slug, codigo, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("reservas")]
    [ProducesResponseType(typeof(PublicReservaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicReservaDto>> CreateReserva(
        string slug,
        CreateReservaPublicaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await reservaPublicaService.CreateReservaAsync(GetCurrentUser(), slug, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return Created(string.Empty, result.Data);
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
