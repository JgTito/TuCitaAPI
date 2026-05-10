using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Common;
using TuCita.Application.Invitaciones;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/mis-invitaciones")]
[Authorize]
public sealed class MisInvitacionesController(IInvitacionNegocioService invitacionService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InvitacionNegocioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InvitacionNegocioDto>>> GetAll(
        [FromQuery] MisInvitacionesQuery query,
        CancellationToken cancellationToken)
    {
        var invitations = await invitacionService.GetMineAsync(GetCurrentUser(), query, cancellationToken);
        return Ok(invitations);
    }

    [HttpGet("{idInvitacion:int}")]
    [ProducesResponseType(typeof(InvitacionNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvitacionNegocioDto>> GetById(
        int idInvitacion,
        CancellationToken cancellationToken)
    {
        var result = await invitacionService.GetMineByIdAsync(GetCurrentUser(), idInvitacion, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{idInvitacion:int}/aceptar")]
    [ProducesResponseType(typeof(InvitacionNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvitacionNegocioDto>> Accept(
        int idInvitacion,
        CancellationToken cancellationToken)
    {
        var result = await invitacionService.AcceptMineAsync(GetCurrentUser(), idInvitacion, cancellationToken);
        return ToActionResult(result);
    }
}
