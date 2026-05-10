using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.CentroOperativo;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/centro-operativo")]
[Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
public sealed class CentroOperativoController(ICentroOperativoService centroOperativoService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CentroOperativoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CentroOperativoDto>> Get(
        int idNegocio,
        [FromQuery] CentroOperativoQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await centroOperativoService.GetAsync(
            GetCurrentUser(),
            idNegocio,
            query,
            cancellationToken);

        return ToActionResult(result);
    }
}
