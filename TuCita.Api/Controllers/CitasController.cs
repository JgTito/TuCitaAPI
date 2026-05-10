using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.Citas;
using TuCita.Application.Common;
using TuCita.Application.Disponibilidad;
using TuCita.Application.Pagos;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/citas")]
[Authorize(Policy = TuCitaPolicies.BusinessProfessional)]
public sealed class CitasController(
    ICitaService citaService,
    IPagoFlowService pagoFlowService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CitaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CitaDto>>> GetAll(
        int idNegocio,
        [FromQuery] CitaQuery query,
        CancellationToken cancellationToken)
    {
        var citas = await citaService.GetAllAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return Ok(citas);
    }

    [HttpGet("{idCita:int}")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> GetById(
        int idNegocio,
        int idCita,
        CancellationToken cancellationToken)
    {
        var result = await citaService.GetByIdAsync(GetCurrentUser(), idNegocio, idCita, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{idCita:int}/disponibilidad-edicion")]
    [ProducesResponseType(typeof(DisponibilidadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DisponibilidadDto>> GetDisponibilidadEdicion(
        int idNegocio,
        int idCita,
        [FromQuery] DisponibilidadEdicionCitaQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await citaService.GetDisponibilidadEdicionAsync(
            GetCurrentUser(),
            idNegocio,
            idCita,
            query,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("{idCita:int}/historial")]
    [ProducesResponseType(typeof(CitaHistorialTimelineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaHistorialTimelineDto>> GetHistorial(
        int idNegocio,
        int idCita,
        CancellationToken cancellationToken)
    {
        var result = await citaService.GetHistorialAsync(GetCurrentUser(), idNegocio, idCita, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{idCita:int}/pagos")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(CitaPagosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaPagosDto>> GetPagos(
        int idNegocio,
        int idCita,
        CancellationToken cancellationToken)
    {
        var result = await pagoFlowService.GetPagosCitaNegocioAsync(
            GetCurrentUser(),
            idNegocio,
            idCita,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> Create(
        int idNegocio,
        CreateCitaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await citaService.CreateAsync(GetCurrentUser(), idNegocio, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { idNegocio, idCita = result.Data.IdCita },
            result.Data);
    }

    [HttpPut("{idCita:int}")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> Update(
        int idNegocio,
        int idCita,
        UpdateCitaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await citaService.UpdateAsync(GetCurrentUser(), idNegocio, idCita, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idCita:int}/reagendar")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> Reagendar(
        int idNegocio,
        int idCita,
        ReagendarCitaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await citaService.ReagendarAsync(GetCurrentUser(), idNegocio, idCita, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idCita:int}/estado")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> CambiarEstado(
        int idNegocio,
        int idCita,
        ChangeEstadoCitaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await citaService.CambiarEstadoAsync(GetCurrentUser(), idNegocio, idCita, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idCita:int}/confirmar")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> Confirmar(
        int idNegocio,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await citaService.ConfirmarAsync(GetCurrentUser(), idNegocio, idCita, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idCita:int}/cancelar")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> Cancelar(
        int idNegocio,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await citaService.CancelarAsync(GetCurrentUser(), idNegocio, idCita, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idCita:int}/marcar-atendida")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> MarcarAtendida(
        int idNegocio,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await citaService.MarcarAtendidaAsync(GetCurrentUser(), idNegocio, idCita, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idCita:int}/marcar-no-asistio")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> MarcarNoAsistio(
        int idNegocio,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await citaService.MarcarNoAsistioAsync(GetCurrentUser(), idNegocio, idCita, request, cancellationToken);
        return ToActionResult(result);
    }
}
