using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Common;
using TuCita.Application.Notificaciones;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/notificaciones")]
[Authorize(Roles = "SuperAdmin,Owner,Admin")]
public sealed class NotificacionesController(INotificacionService notificacionService) : TuCitaControllerBase
{
    [HttpPost("procesar-pendientes")]
    [ProducesResponseType(typeof(ProcesarNotificacionesResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProcesarNotificacionesResultDto>> ProcesarPendientes(
        [FromQuery] int? idNegocio,
        [FromQuery] int maxNotificaciones,
        CancellationToken cancellationToken)
    {
        var result = await notificacionService.ProcesarPendientesAsync(
            GetCurrentUser(),
            idNegocio,
            maxNotificaciones <= 0 ? 100 : maxNotificaciones,
            cancellationToken);

        return ToActionResult(result);
    }
}
