using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.Common;
using TuCita.Application.Pagos;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/pagos")]
[Authorize(Policy = TuCitaPolicies.BusinessAgenda)]
public sealed class PagosController(IPagoFlowService pagoFlowService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagoNegocioListadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagoNegocioListadoDto>> GetAll(
        int idNegocio,
        [FromQuery] PagoQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await pagoFlowService.GetPagosNegocioAsync(
            GetCurrentUser(),
            idNegocio,
            query,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("select/estados")]
    [ProducesResponseType(typeof(IReadOnlyCollection<EstadoPagoFiltroDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<EstadoPagoFiltroDto>>> GetEstadosSelect(
        int idNegocio,
        [FromQuery] PagoFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await pagoFlowService.GetEstadosPagoSelectAsync(
            GetCurrentUser(),
            idNegocio,
            query,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("select/proveedores")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PagoProveedorFiltroDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<PagoProveedorFiltroDto>>> GetProveedoresSelect(
        int idNegocio,
        [FromQuery] PagoFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await pagoFlowService.GetProveedoresSelectAsync(
            GetCurrentUser(),
            idNegocio,
            query,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("select/origenes")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PagoOrigenFiltroDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<PagoOrigenFiltroDto>>> GetOrigenesSelect(
        int idNegocio,
        [FromQuery] PagoFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await pagoFlowService.GetOrigenesSelectAsync(
            GetCurrentUser(),
            idNegocio,
            query,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("select/metodos")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PagoMetodoFiltroDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<PagoMetodoFiltroDto>>> GetMetodosSelect(
        int idNegocio,
        [FromQuery] PagoFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await pagoFlowService.GetMetodosSelectAsync(
            GetCurrentUser(),
            idNegocio,
            query,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("select/clientes")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PagoClienteFiltroDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<PagoClienteFiltroDto>>> GetClientesSelect(
        int idNegocio,
        [FromQuery] PagoFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await pagoFlowService.GetClientesSelectAsync(
            GetCurrentUser(),
            idNegocio,
            query,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("select/servicios")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PagoServicioFiltroDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<PagoServicioFiltroDto>>> GetServiciosSelect(
        int idNegocio,
        [FromQuery] PagoFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await pagoFlowService.GetServiciosSelectAsync(
            GetCurrentUser(),
            idNegocio,
            query,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("select/citas")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PagoCitaFiltroDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<PagoCitaFiltroDto>>> GetCitasSelect(
        int idNegocio,
        [FromQuery] PagoFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await pagoFlowService.GetCitasSelectAsync(
            GetCurrentUser(),
            idNegocio,
            query,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("{idPago:int}")]
    [ProducesResponseType(typeof(PagoNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagoNegocioDto>> GetById(
        int idNegocio,
        int idPago,
        CancellationToken cancellationToken)
    {
        var result = await pagoFlowService.GetPagoNegocioByIdAsync(
            GetCurrentUser(),
            idNegocio,
            idPago,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("{idPago:int}/comprobante")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DescargarComprobante(
        int idNegocio,
        int idPago,
        CancellationToken cancellationToken)
    {
        var result = await pagoFlowService.DescargarComprobanteNegocioAsync(
            GetCurrentUser(),
            idNegocio,
            idPago,
            cancellationToken);

        return ToFileResult(result);
    }

    [HttpGet("{idPago:int}/historial")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PagoHistorialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<PagoHistorialDto>>> GetHistorial(
        int idNegocio,
        int idPago,
        CancellationToken cancellationToken)
    {
        var result = await pagoFlowService.GetHistorialPagoAsync(
            GetCurrentUser(),
            idNegocio,
            idPago,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{idPago:int}/devoluciones")]
    [ProducesResponseType(typeof(PagoNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagoNegocioDto>> RegistrarDevolucion(
        int idNegocio,
        int idPago,
        [FromBody] RegistrarDevolucionPagoRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await pagoFlowService.RegistrarDevolucionAsync(
            GetCurrentUser(),
            idNegocio,
            idPago,
            request,
            cancellationToken);

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
