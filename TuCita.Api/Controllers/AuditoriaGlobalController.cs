using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/auditoria")]
[Authorize(Roles = "SuperAdmin")]
public sealed class AuditoriaGlobalController(IAuditoriaService auditoriaService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditoriaEventoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AuditoriaEventoDto>>> GetGlobal(
        [FromQuery] AuditoriaQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await auditoriaService.GetGlobalAsync(
            GetCurrentUser(),
            query,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("select/categorias")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AuditoriaFiltroSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>> GetCategoriasSelect(
        [FromQuery] int? idNegocio,
        [FromQuery] AuditoriaFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await auditoriaService.GetCategoriasSelectAsync(
            GetCurrentUser(),
            idNegocio,
            query,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("select/acciones")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AuditoriaFiltroSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>> GetAccionesSelect(
        [FromQuery] int? idNegocio,
        [FromQuery] AuditoriaFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await auditoriaService.GetAccionesSelectAsync(
            GetCurrentUser(),
            idNegocio,
            query,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("select/entidades")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AuditoriaFiltroSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>> GetEntidadesSelect(
        [FromQuery] int? idNegocio,
        [FromQuery] AuditoriaFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await auditoriaService.GetEntidadesSelectAsync(
            GetCurrentUser(),
            idNegocio,
            query,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("select/usuarios")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AuditoriaFiltroSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<AuditoriaFiltroSelectDto>>> GetUsuariosSelect(
        [FromQuery] int? idNegocio,
        [FromQuery] AuditoriaFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await auditoriaService.GetUsuariosSelectAsync(
            GetCurrentUser(),
            idNegocio,
            query,
            cancellationToken);

        return ToActionResult(result);
    }
}
