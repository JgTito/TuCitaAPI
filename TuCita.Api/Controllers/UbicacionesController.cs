using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Ubicaciones;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/ubicaciones")]
[AllowAnonymous]
public sealed class UbicacionesController(IUbicacionService ubicacionService) : TuCitaControllerBase
{
    [HttpGet("paises/select")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PaisSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<PaisSelectDto>>> GetPaisesSelect(
        [FromQuery] PaisSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var items = await ubicacionService.GetPaisesSelectAsync(query, cancellationToken);
        return Ok(items);
    }

    [HttpGet("ciudades/select")]
    [ProducesResponseType(typeof(IReadOnlyCollection<CiudadSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<CiudadSelectDto>>> GetCiudadesSelect(
        [FromQuery] CiudadSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var items = await ubicacionService.GetCiudadesSelectAsync(query, cancellationToken);
        return Ok(items);
    }

    [HttpGet("comunas/select")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ComunaSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<ComunaSelectDto>>> GetComunasSelect(
        [FromQuery] ComunaSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var items = await ubicacionService.GetComunasSelectAsync(query, cancellationToken);
        return Ok(items);
    }
}
