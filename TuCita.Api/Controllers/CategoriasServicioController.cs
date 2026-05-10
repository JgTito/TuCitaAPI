using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.CategoriasServicio;
using TuCita.Application.Common;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/categorias-servicio")]
[Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
public sealed class CategoriasServicioController(ICategoriaServicioService categoriaServicioService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CategoriaServicioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CategoriaServicioDto>>> GetAll(
        int idNegocio,
        [FromQuery] CategoriaServicioQuery query,
        CancellationToken cancellationToken)
    {
        var categorias = await categoriaServicioService.GetAllAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return Ok(categorias);
    }

    [HttpGet("{idCategoriaServicio:int}")]
    [ProducesResponseType(typeof(CategoriaServicioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoriaServicioDto>> GetById(
        int idNegocio,
        int idCategoriaServicio,
        CancellationToken cancellationToken)
    {
        var result = await categoriaServicioService.GetByIdAsync(GetCurrentUser(), idNegocio, idCategoriaServicio, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CategoriaServicioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoriaServicioDto>> Create(
        int idNegocio,
        CreateCategoriaServicioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await categoriaServicioService.CreateAsync(GetCurrentUser(), idNegocio, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { idNegocio, idCategoriaServicio = result.Data.IdCategoriaServicio },
            result.Data);
    }

    [HttpPut("{idCategoriaServicio:int}")]
    [ProducesResponseType(typeof(CategoriaServicioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoriaServicioDto>> Update(
        int idNegocio,
        int idCategoriaServicio,
        UpdateCategoriaServicioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await categoriaServicioService.UpdateAsync(GetCurrentUser(), idNegocio, idCategoriaServicio, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idCategoriaServicio:int}/activar")]
    [ProducesResponseType(typeof(CategoriaServicioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoriaServicioDto>> Activate(
        int idNegocio,
        int idCategoriaServicio,
        CancellationToken cancellationToken)
    {
        var result = await categoriaServicioService.SetActiveAsync(
            GetCurrentUser(),
            idNegocio,
            idCategoriaServicio,
            activo: true,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPatch("{idCategoriaServicio:int}/desactivar")]
    [ProducesResponseType(typeof(CategoriaServicioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoriaServicioDto>> Deactivate(
        int idNegocio,
        int idCategoriaServicio,
        CancellationToken cancellationToken)
    {
        var result = await categoriaServicioService.SetActiveAsync(
            GetCurrentUser(),
            idNegocio,
            idCategoriaServicio,
            activo: false,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{idCategoriaServicio:int}")]
    [ProducesResponseType(typeof(CategoriaServicioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoriaServicioDto>> Delete(
        int idNegocio,
        int idCategoriaServicio,
        CancellationToken cancellationToken)
    {
        var result = await categoriaServicioService.DeleteAsync(GetCurrentUser(), idNegocio, idCategoriaServicio, cancellationToken);
        return ToActionResult(result);
    }
}
