using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.Common;
using TuCita.Application.Invitaciones;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/invitaciones")]
[Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
public sealed class InvitacionesNegocioController(IInvitacionNegocioService invitacionService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InvitacionNegocioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InvitacionNegocioDto>>> GetAll(
        int idNegocio,
        [FromQuery] InvitacionNegocioQuery query,
        CancellationToken cancellationToken)
    {
        var invitations = await invitacionService.GetAllAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return Ok(invitations);
    }

    [HttpGet("{idInvitacion:int}")]
    [ProducesResponseType(typeof(InvitacionNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvitacionNegocioDto>> GetById(
        int idNegocio,
        int idInvitacion,
        CancellationToken cancellationToken)
    {
        var result = await invitacionService.GetByIdAsync(GetCurrentUser(), idNegocio, idInvitacion, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(InvitacionCreadaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvitacionCreadaDto>> Create(
        int idNegocio,
        CreateInvitacionNegocioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await invitacionService.CreateAsync(GetCurrentUser(), idNegocio, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { idNegocio, idInvitacion = result.Data.Invitacion.IdInvitacionNegocio },
            result.Data);
    }

    [HttpPost("{idInvitacion:int}/cancelar")]
    [ProducesResponseType(typeof(InvitacionNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvitacionNegocioDto>> Cancel(
        int idNegocio,
        int idInvitacion,
        CancelInvitacionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await invitacionService.CancelAsync(GetCurrentUser(), idNegocio, idInvitacion, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{idInvitacion:int}/reenviar")]
    [ProducesResponseType(typeof(InvitacionCreadaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvitacionCreadaDto>> Resend(
        int idNegocio,
        int idInvitacion,
        CancellationToken cancellationToken)
    {
        var result = await invitacionService.ResendAsync(GetCurrentUser(), idNegocio, idInvitacion, cancellationToken);
        return ToActionResult(result);
    }
}
