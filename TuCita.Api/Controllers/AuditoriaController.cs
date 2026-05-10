using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/auditoria")]
[Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
public sealed class AuditoriaController(IAuditoriaService auditoriaService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditoriaEventoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<AuditoriaEventoDto>>> GetAll(
        int idNegocio,
        [FromQuery] AuditoriaQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await auditoriaService.GetByNegocioAsync(
            GetCurrentUser(),
            idNegocio,
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
        int idNegocio,
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
        int idNegocio,
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
        int idNegocio,
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
        int idNegocio,
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
