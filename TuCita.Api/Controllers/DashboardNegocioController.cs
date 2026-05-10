using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.Dashboard;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/dashboard")]
[Authorize(Policy = TuCitaPolicies.BusinessAgenda)]
public sealed class DashboardNegocioController(IDashboardNegocioService dashboardNegocioService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(DashboardNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DashboardNegocioDto>> Get(
        int idNegocio,
        [FromQuery] DashboardNegocioQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await dashboardNegocioService.GetAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("pagos")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(DashboardPagosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DashboardPagosDto>> GetPagos(
        int idNegocio,
        [FromQuery] DashboardPagosQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await dashboardNegocioService.GetPagosAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return ToActionResult(result);
    }
}
