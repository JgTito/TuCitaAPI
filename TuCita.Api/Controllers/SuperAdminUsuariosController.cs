using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Application.Common;
using TuCita.Application.SuperAdminUsuarios;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/super-admin/usuarios")]
[Authorize(Roles = "SuperAdmin")]
public sealed class SuperAdminUsuariosController(ISuperAdminUsuarioService usuarioService) : TuCitaControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SuperAdminUsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<SuperAdminUsuarioDto>>> GetAll(
        [FromQuery] SuperAdminUsuarioQuery query,
        CancellationToken cancellationToken)
    {
        var result = await usuarioService.GetAllAsync(GetCurrentUser(), query, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("select/roles")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SuperAdminRolSelectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<SuperAdminRolSelectDto>>> GetRolesSelect(
        [FromQuery] SuperAdminRolSelectQuery query,
        CancellationToken cancellationToken)
    {
        var result = await usuarioService.GetRolesSelectAsync(GetCurrentUser(), query, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(SuperAdminUsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SuperAdminUsuarioDto>> GetById(
        string userId,
        CancellationToken cancellationToken)
    {
        var result = await usuarioService.GetByIdAsync(GetCurrentUser(), userId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SuperAdminUsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SuperAdminUsuarioDto>> Create(
        CreateSuperAdminUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await usuarioService.CreateAsync(GetCurrentUser(), request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetById), new { userId = result.Data.UserId }, result.Data);
    }

    [HttpPut("{userId}")]
    [ProducesResponseType(typeof(SuperAdminUsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SuperAdminUsuarioDto>> Update(
        string userId,
        UpdateSuperAdminUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await usuarioService.UpdateAsync(GetCurrentUser(), userId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{userId}/activar")]
    [ProducesResponseType(typeof(SuperAdminUsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SuperAdminUsuarioDto>> Activate(
        string userId,
        CancellationToken cancellationToken)
    {
        var result = await usuarioService.SetActiveAsync(GetCurrentUser(), userId, activo: true, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{userId}/desactivar")]
    [ProducesResponseType(typeof(SuperAdminUsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SuperAdminUsuarioDto>> Deactivate(
        string userId,
        CancellationToken cancellationToken)
    {
        var result = await usuarioService.SetActiveAsync(GetCurrentUser(), userId, activo: false, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{userId}/roles")]
    [ProducesResponseType(typeof(SuperAdminUsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SuperAdminUsuarioDto>> UpdateRoles(
        string userId,
        UpdateSuperAdminUsuarioRolesRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await usuarioService.UpdateRolesAsync(GetCurrentUser(), userId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{userId}/password")]
    [ProducesResponseType(typeof(SuperAdminUsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SuperAdminUsuarioDto>> ResetPassword(
        string userId,
        ResetSuperAdminUsuarioPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await usuarioService.ResetPasswordAsync(GetCurrentUser(), userId, request, cancellationToken);
        return ToActionResult(result);
    }
}
