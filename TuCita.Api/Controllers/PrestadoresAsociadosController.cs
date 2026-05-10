using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Common;
using TuCita.Application.Prestadores;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/prestadores")]
[Authorize]
public sealed class PrestadoresAsociadosController(IPrestadorService prestadorService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PrestadorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PrestadorDto>>> GetFromAssociatedBusinesses(
        [FromQuery] PrestadorQuery query,
        CancellationToken cancellationToken)
    {
        var prestadores = await prestadorService.GetFromAssociatedBusinessesAsync(
            GetCurrentUser(),
            query,
            cancellationToken);

        return Ok(prestadores);
    }

    [HttpGet("select")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PrestadorSelectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PrestadorSelectDto>>> GetSelectFromAssociatedBusinesses(
        [FromQuery] PrestadorSelectQuery query,
        CancellationToken cancellationToken)
    {
        var prestadores = await prestadorService.GetSelectFromAssociatedBusinessesAsync(
            GetCurrentUser(),
            query,
            cancellationToken);

        return Ok(prestadores);
    }
}
