using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Common;
using TuCita.Application.Disponibilidad;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/disponibilidad")]
[AllowAnonymous]
public sealed class DisponibilidadController(IDisponibilidadService disponibilidadService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(DisponibilidadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DisponibilidadDto>> GetDisponibilidad(
        int idNegocio,
        [FromQuery] DisponibilidadQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await disponibilidadService.GetDisponibilidadAsync(idNegocio, query, cancellationToken);
        return ToActionResult(result);
    }
}
