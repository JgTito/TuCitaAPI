using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuCita.Api.Requests.Auth;
using TuCita.Api.Storage;
using TuCita.Application.Auth;

namespace TuCita.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class AuthController(
    IAuthService authService,
    IFileStorageService fileStorageService) : TuCitaControllerBase
{
    [HttpPost("register-cliente")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> RegisterCliente(
        [FromForm] RegisterFormRequest request,
        CancellationToken cancellationToken)
    {
        return await RegisterClienteInternalAsync(request, cancellationToken);
    }

    [HttpPost("register")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<AuthResponse>> Register(
        [FromForm] RegisterFormRequest request,
        CancellationToken cancellationToken)
    {
        return RegisterClienteInternalAsync(request, cancellationToken);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await authService.LoginAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return Unauthorized(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> RefreshToken(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await authService.RefreshTokenAsync(request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return Unauthorized(result.Errors);
        }

        return Ok(result.Data);
    }

    private async Task<ActionResult<AuthResponse>> RegisterClienteInternalAsync(
        RegisterFormRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var avatarUrl = await SaveAvatarOrAddModelErrorAsync(request.Avatar, cancellationToken);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var registerRequest = new RegisterRequest(
            request.Email,
            request.Password,
            request.Nombre,
            request.Apellido,
            request.Rut,
            request.FechaNacimiento,
            request.TelefonoAlternativo,
            request.Direccion,
            request.IdComuna,
            request.AceptaTerminos,
            request.AceptaMarketing);

        var result = await authService.RegisterClienteAsync(registerRequest, avatarUrl, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return ToValidationProblem<AuthResponse>(result.Errors);
        }

        return CreatedAtAction(nameof(RegisterCliente), new { userId = result.Data.UserId }, result.Data);
    }

    private async Task<string?> SaveAvatarOrAddModelErrorAsync(
        IFormFile? avatar,
        CancellationToken cancellationToken)
    {
        try
        {
            return await fileStorageService.SaveUserAvatarAsync(avatar, cancellationToken);
        }
        catch (FileStorageValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return null;
        }
    }
}
