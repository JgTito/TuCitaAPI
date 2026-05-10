using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Common;
using TuCita.Application.Resenas;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios-publicos/{slug}/resenas")]
[AllowAnonymous]
public sealed class ResenasPublicasController(IResenaNegocioService resenaService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ResenaPublicaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ResenaPublicaDto>>> GetPublicas(
        string slug,
        [FromQuery] ResenaPublicaQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await resenaService.GetPublicasAsync(slug, query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ResenaResumenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResenaResumenDto>> GetResumenPublico(
        string slug,
        CancellationToken cancellationToken)
    {
        var result = await resenaService.GetResumenPublicoAsync(slug, cancellationToken);
        return ToActionResult(result);
    }
}
