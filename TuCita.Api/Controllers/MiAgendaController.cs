using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Agenda;
using TuCita.Application.Citas;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/mi-agenda")]
[Authorize]
public sealed class MiAgendaController(
    IAgendaService agendaService,
    ICitaService citaService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(MiAgendaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MiAgendaDto>> Get(
        [FromQuery] MiAgendaQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await agendaService.GetMiAgendaAsync(GetCurrentUser(), query, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("citas/{idCita:int}")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> GetCita(
        int idCita,
        CancellationToken cancellationToken)
    {
        var result = await citaService.GetMiAgendaCitaByIdAsync(GetCurrentUser(), idCita, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("citas/{idCita:int}/movimientos-disponibles")]
    [ProducesResponseType(typeof(MovimientosDisponiblesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovimientosDisponiblesDto>> GetMovimientosDisponibles(
        int idCita,
        [FromQuery] MovimientosDisponiblesQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await agendaService.GetMovimientosDisponiblesAsync(GetCurrentUser(), idCita, query, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("citas/{idCita:int}/reagendar")]
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

        var result = await citaService.ReagendarMiAgendaCitaAsync(GetCurrentUser(), idCita, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("citas/{idCita:int}/mover")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> Mover(
        int idCita,
        ReagendarCitaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await citaService.ReagendarMiAgendaCitaAsync(GetCurrentUser(), idCita, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("citas/{idCita:int}/confirmar")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> Confirmar(
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await citaService.ConfirmarMiAgendaCitaAsync(GetCurrentUser(), idCita, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("citas/{idCita:int}/cancelar")]
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

        var result = await citaService.CancelarMiAgendaCitaAsync(GetCurrentUser(), idCita, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("citas/{idCita:int}/marcar-atendida")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> MarcarAtendida(
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await citaService.MarcarAtendidaMiAgendaCitaAsync(GetCurrentUser(), idCita, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("citas/{idCita:int}/marcar-no-asistio")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> MarcarNoAsistio(
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await citaService.MarcarNoAsistioMiAgendaCitaAsync(GetCurrentUser(), idCita, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("citas/{idCita:int}/nota-interna")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CitaDto>> ActualizarNotaInterna(
        int idCita,
        UpdateNotaInternaCitaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await citaService.ActualizarNotaInternaMiAgendaCitaAsync(GetCurrentUser(), idCita, request, cancellationToken);
        return ToActionResult(result);
    }
}
