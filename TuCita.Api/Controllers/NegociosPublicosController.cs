using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Common;
using TuCita.Application.ReservasPublicas;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios-publicos")]
[AllowAnonymous]
public sealed class NegociosPublicosController(IReservaPublicaService reservaPublicaService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PublicNegocioSearchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PublicNegocioSearchDto>>> Search(
        [FromQuery] PublicNegocioQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await reservaPublicaService.SearchNegociosAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(PublicNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicNegocioDto>> GetBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        var result = await reservaPublicaService.GetNegocioAsync(slug, cancellationToken);
        return ToActionResult(result);
    }
}
