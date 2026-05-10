using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.Common;
using TuCita.Application.Resenas;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/resenas")]
[Authorize(Policy = TuCitaPolicies.BusinessProfessional)]
public sealed class ResenasNegocioController(IResenaNegocioService resenaService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ResenaNegocioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<ResenaNegocioDto>>> GetAll(
        int idNegocio,
        [FromQuery] ResenaNegocioQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await resenaService.GetByNegocioAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ResenaResumenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResenaResumenDto>> GetResumen(
        int idNegocio,
        CancellationToken cancellationToken)
    {
        var result = await resenaService.GetResumenNegocioAsync(GetCurrentUser(), idNegocio, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("reputacion")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(ReputacionNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReputacionNegocioDto>> GetReputacion(
        int idNegocio,
        [FromQuery] ReputacionNegocioQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await resenaService.GetReputacionAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("estados-select")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ResenaEstadoSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<ResenaEstadoSelectDto>>> GetEstadosSelect(
        int idNegocio,
        CancellationToken cancellationToken)
    {
        var result = await resenaService.GetEstadosSelectAsync(GetCurrentUser(), idNegocio, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("puntuaciones-select")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ResenaPuntuacionSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<ResenaPuntuacionSelectDto>>> GetPuntuacionesSelect(
        int idNegocio,
        CancellationToken cancellationToken)
    {
        var result = await resenaService.GetPuntuacionesSelectAsync(GetCurrentUser(), idNegocio, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("configuracion")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(ConfiguracionResenaNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConfiguracionResenaNegocioDto>> GetConfiguracion(
        int idNegocio,
        CancellationToken cancellationToken)
    {
        var result = await resenaService.GetConfiguracionAsync(GetCurrentUser(), idNegocio, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("configuracion")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(ConfiguracionResenaNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConfiguracionResenaNegocioDto>> UpdateConfiguracion(
        int idNegocio,
        UpdateConfiguracionResenaNegocioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await resenaService.UpdateConfiguracionAsync(GetCurrentUser(), idNegocio, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idResena:int}/aprobar")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(ResenaNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResenaNegocioDto>> Aprobar(
        int idNegocio,
        int idResena,
        ModerarResenaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await resenaService.AprobarAsync(GetCurrentUser(), idNegocio, idResena, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idResena:int}/rechazar")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(ResenaNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResenaNegocioDto>> Rechazar(
        int idNegocio,
        int idResena,
        ModerarResenaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await resenaService.RechazarAsync(GetCurrentUser(), idNegocio, idResena, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idResena:int}/ocultar")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(ResenaNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResenaNegocioDto>> Ocultar(
        int idNegocio,
        int idResena,
        ModerarResenaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await resenaService.OcultarAsync(GetCurrentUser(), idNegocio, idResena, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{idResena:int}/respuesta")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(ResenaNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResenaNegocioDto>> Responder(
        int idNegocio,
        int idResena,
        ResponderResenaRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await resenaService.ResponderAsync(GetCurrentUser(), idNegocio, idResena, request, cancellationToken);
        return ToActionResult(result);
    }
}
