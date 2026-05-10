using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.Common;
using TuCita.Application.Servicios;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/servicios")]
[Authorize]
public sealed class ServiciosController(IServicioService servicioService) : TuCitaControllerBase
{
    [HttpGet]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(PagedResult<ServicioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ServicioDto>>> GetAll(
        int idNegocio,
        [FromQuery] ServicioQuery query,
        CancellationToken cancellationToken)
    {
        var servicios = await servicioService.GetAllAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return Ok(servicios);
    }

    [HttpGet("select")]
    [Authorize(Policy = TuCitaPolicies.BusinessProfessional)]
    [ProducesResponseType(typeof(IReadOnlyCollection<ServicioSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<ServicioSelectDto>>> GetSelect(
        int idNegocio,
        [FromQuery] ServicioSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var servicios = await servicioService.GetSelectAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return Ok(servicios);
    }

    [HttpGet("{idServicio:int}")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(ServicioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServicioDto>> GetById(
        int idNegocio,
        int idServicio,
        CancellationToken cancellationToken)
    {
        var result = await servicioService.GetByIdAsync(GetCurrentUser(), idNegocio, idServicio, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(ServicioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServicioDto>> Create(
        int idNegocio,
        CreateServicioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await servicioService.CreateAsync(GetCurrentUser(), idNegocio, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { idNegocio, idServicio = result.Data.IdServicio },
            result.Data);
    }

    [HttpPut("{idServicio:int}")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(ServicioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServicioDto>> Update(
        int idNegocio,
        int idServicio,
        UpdateServicioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await servicioService.UpdateAsync(GetCurrentUser(), idNegocio, idServicio, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idServicio:int}/activar")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(ServicioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServicioDto>> Activate(
        int idNegocio,
        int idServicio,
        CancellationToken cancellationToken)
    {
        var result = await servicioService.SetActiveAsync(
            GetCurrentUser(),
            idNegocio,
            idServicio,
            activo: true,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPatch("{idServicio:int}/desactivar")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(ServicioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServicioDto>> Deactivate(
        int idNegocio,
        int idServicio,
        CancellationToken cancellationToken)
    {
        var result = await servicioService.SetActiveAsync(
            GetCurrentUser(),
            idNegocio,
            idServicio,
            activo: false,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{idServicio:int}")]
    [Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
    [ProducesResponseType(typeof(ServicioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServicioDto>> Delete(
        int idNegocio,
        int idServicio,
        CancellationToken cancellationToken)
    {
        var result = await servicioService.DeleteAsync(GetCurrentUser(), idNegocio, idServicio, cancellationToken);
        return ToActionResult(result);
    }
}
