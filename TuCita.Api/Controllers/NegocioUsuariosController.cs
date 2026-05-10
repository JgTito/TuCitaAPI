using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Authorization;
using TuCita.Application.Common;
using TuCita.Application.NegocioUsuarios;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/negocios/{idNegocio:int}/usuarios")]
[Authorize(Policy = TuCitaPolicies.BusinessOwnerAdmin)]
public sealed class NegocioUsuariosController(INegocioUsuarioService negocioUsuarioService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NegocioUsuarioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NegocioUsuarioDto>>> GetAll(
        int idNegocio,
        [FromQuery] NegocioUsuarioQuery query,
        CancellationToken cancellationToken)
    {
        var usuarios = await negocioUsuarioService.GetAllAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return Ok(usuarios);
    }

    [HttpGet("select")]
    [ProducesResponseType(typeof(IReadOnlyCollection<NegocioUsuarioSelectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<NegocioUsuarioSelectDto>>> GetSelect(
        int idNegocio,
        [FromQuery] NegocioUsuarioSelectQuery query,
        CancellationToken cancellationToken)
    {
        var usuarios = await negocioUsuarioService.GetSelectAsync(GetCurrentUser(), idNegocio, query, cancellationToken);
        return Ok(usuarios);
    }

    [HttpGet("{idNegocioUsuario:int}")]
    [ProducesResponseType(typeof(NegocioUsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NegocioUsuarioDto>> GetById(
        int idNegocio,
        int idNegocioUsuario,
        CancellationToken cancellationToken)
    {
        var result = await negocioUsuarioService.GetByIdAsync(GetCurrentUser(), idNegocio, idNegocioUsuario, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(NegocioUsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NegocioUsuarioDto>> Create(
        int idNegocio,
        CreateNegocioUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await negocioUsuarioService.CreateAsync(GetCurrentUser(), idNegocio, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { idNegocio, idNegocioUsuario = result.Data.IdNegocioUsuario },
            result.Data);
    }

    [HttpPut("{idNegocioUsuario:int}")]
    [ProducesResponseType(typeof(NegocioUsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NegocioUsuarioDto>> Update(
        int idNegocio,
        int idNegocioUsuario,
        UpdateNegocioUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await negocioUsuarioService.UpdateAsync(GetCurrentUser(), idNegocio, idNegocioUsuario, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{idNegocioUsuario:int}/activar")]
    [ProducesResponseType(typeof(NegocioUsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NegocioUsuarioDto>> Activate(
        int idNegocio,
        int idNegocioUsuario,
        CancellationToken cancellationToken)
    {
        var result = await negocioUsuarioService.SetActiveAsync(
            GetCurrentUser(),
            idNegocio,
            idNegocioUsuario,
            activo: true,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPatch("{idNegocioUsuario:int}/desactivar")]
    [ProducesResponseType(typeof(NegocioUsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NegocioUsuarioDto>> Deactivate(
        int idNegocio,
        int idNegocioUsuario,
        CancellationToken cancellationToken)
    {
        var result = await negocioUsuarioService.SetActiveAsync(
            GetCurrentUser(),
            idNegocio,
            idNegocioUsuario,
            activo: false,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{idNegocioUsuario:int}")]
    [ProducesResponseType(typeof(NegocioUsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NegocioUsuarioDto>> Delete(
        int idNegocio,
        int idNegocioUsuario,
        CancellationToken cancellationToken)
    {
        var result = await negocioUsuarioService.DeleteAsync(GetCurrentUser(), idNegocio, idNegocioUsuario, cancellationToken);
        return ToActionResult(result);
    }
}
