using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.ReservasPublicas;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/reservas-publicas/{slug}")]
[Authorize]
public sealed class ReservasPublicasPerfilController(IReservaPublicaService reservaPublicaService) : TuCitaControllerBase
{
    [HttpGet("mis-datos")]
    [ProducesResponseType(typeof(PublicReservaMisDatosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicReservaMisDatosDto>> GetMisDatosReserva(
        string slug,
        CancellationToken cancellationToken)
    {
        var result = await reservaPublicaService.GetMisDatosReservaAsync(GetCurrentUser(), slug, cancellationToken);
        return ToActionResult(result);
    }
}
