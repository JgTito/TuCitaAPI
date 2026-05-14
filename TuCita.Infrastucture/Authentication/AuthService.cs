using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TuCita.Application.Auditoria;
using TuCita.Application.Auth;
using TuCita.Application.Common;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;
using TuCita.Infrastucture.UsuariosPerfil;

namespace TuCita.Infrastucture.Authentication;

public sealed class AuthService(
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<JwtOptions> jwtOptions,
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : IAuthService
{
    private const string DefaultCustomerRole = "Cliente";

    public async Task<AuthResult> RegisterClienteAsync(
        RegisterRequest request,
        string? avatarUrl,
        CancellationToken cancellationToken)
    {
        var validationErrors = DataAnnotationsValidator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return ValidationFailure(validationErrors);
        }

        if (!request.AceptaTerminos)
        {
            return AuthResult.Failure(["Debes aceptar los términos y condiciones para registrarte."]);
        }

        if (!await ComunaIsValidAsync(request.IdComuna, cancellationToken))
        {
            return AuthResult.Failure(["La comuna indicada no existe o no está activa."]);
        }

        var email = request.Email.Trim();
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return AuthResult.Failure(["Ya existe un usuario registrado con ese correo."]);
        }

        var user = new IdentityUser
        {
            Email = email,
            UserName = email
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return AuthResult.Failure(createResult.Errors.Select(error => error.Description));
        }

        await EnsureRoleExistsAsync(DefaultCustomerRole);

        var roleResult = await userManager.AddToRoleAsync(user, DefaultCustomerRole);
        if (!roleResult.Succeeded)
        {
            return AuthResult.Failure(roleResult.Errors.Select(error => error.Description));
        }

        var profile = UsuarioPerfilFactory.Create(
            user.Id,
            request.Nombre,
            request.Apellido,
            request.Rut,
            request.FechaNacimiento,
            avatarUrl,
            request.TelefonoAlternativo,
            request.Direccion,
            request.IdComuna,
            request.AceptaTerminos,
            request.AceptaMarketing);
        var security = UsuarioPerfilFactory.EnsureSecurity(profile);
        security.FechaUltimoLogin = DateTime.UtcNow;
        security.UltimoAcceso = DateTime.UtcNow;
        dbContext.UsuariosPerfil.Add(profile);

        await auditoriaService.RegistrarAsync(
            new CurrentUserContext(user.Id, [DefaultCustomerRole]),
            new AuditoriaRegistro(
                null,
                "Usuarios",
                "RegistrarCliente",
                nameof(IdentityUser),
                user.Id,
                $"Cliente registrado: {email}",
                ValoresNuevos: new
                {
                    user.Id,
                    Email = email,
                    Roles = new[] { DefaultCustomerRole },
                    Perfil = ToProfileAuditSnapshot(profile)
                }),
            cancellationToken);

        var response = await CreateAuthResponseAsync(user, cancellationToken, avatarUrl: avatarUrl);
        return AuthResult.Success(response);
    }

    public async Task<AuthResult> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var validationErrors = DataAnnotationsValidator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return ValidationFailure(validationErrors);
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return InvalidCredentials();
        }

        if (await userManager.IsLockedOutAsync(user) ||
            !await UserIsActiveAsync(user.Id, cancellationToken))
        {
            return InvalidCredentials();
        }

        var passwordIsValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordIsValid)
        {
            return InvalidCredentials();
        }

        await UpdateLoginAuditAsync(user, ipAddress, userAgent, cancellationToken);
        var response = await CreateAuthResponseAsync(user, cancellationToken);
        return AuthResult.Success(response);
    }

    public async Task<AuthResult> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var validationErrors = DataAnnotationsValidator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return ValidationFailure(validationErrors);
        }

        var tokenHash = TokenHasher.Hash(request.RefreshToken);
        var refreshToken = await dbContext.AuthRefreshTokens
            .FirstOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null ||
            refreshToken.FechaRevocacion.HasValue ||
            refreshToken.FechaExpiracion <= DateTime.UtcNow)
        {
            return InvalidRefreshToken();
        }

        var user = await userManager.FindByIdAsync(refreshToken.UserId);
        if (user is null)
        {
            return InvalidRefreshToken();
        }

        if (await userManager.IsLockedOutAsync(user) ||
            !await UserIsActiveAsync(user.Id, cancellationToken))
        {
            return InvalidRefreshToken();
        }

        var response = await CreateAuthResponseAsync(user, cancellationToken, refreshToken);
        return AuthResult.Success(response);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        IdentityUser user,
        CancellationToken cancellationToken,
        AuthRefreshToken? refreshTokenToRevoke = null,
        string? avatarUrl = null)
    {
        var roles = await userManager.GetRolesAsync(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpirationMinutes);
        var token = CreateToken(user, roles, expiresAt);
        var refreshToken = RefreshTokenGenerator.Generate();
        var refreshTokenHash = TokenHasher.Hash(refreshToken);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationDays);

        if (refreshTokenToRevoke is not null)
        {
            refreshTokenToRevoke.FechaRevocacion = DateTime.UtcNow;
            refreshTokenToRevoke.ReemplazadoPorTokenHash = refreshTokenHash;
        }

        dbContext.AuthRefreshTokens.Add(new AuthRefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            FechaCreacion = DateTime.UtcNow,
            FechaExpiracion = refreshTokenExpiresAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            roles.ToArray(),
            token,
            expiresAt,
            refreshToken,
            refreshTokenExpiresAt,
            avatarUrl ?? await GetAvatarUrlAsync(user.Id, cancellationToken));
    }

    private async Task<string?> GetAvatarUrlAsync(string userId, CancellationToken cancellationToken)
    {
        return await dbContext.UsuariosPerfil
            .AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => profile.AvatarUrl)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task UpdateLoginAuditAsync(
        IdentityUser user,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.UsuariosPerfil
            .Include(item => item.Seguridad)
            .FirstOrDefaultAsync(item => item.UserId == user.Id, cancellationToken);

        var now = DateTime.UtcNow;
        if (profile is null)
        {
            profile = UsuarioPerfilFactory.Create(
                user.Id,
                nombre: null,
                apellido: null,
                rut: null,
                fechaNacimiento: null,
                avatarUrl: null,
                telefonoAlternativo: null,
                direccion: null,
                idComuna: null,
                aceptaTerminos: false,
                aceptaMarketing: false);
            var newSecurity = UsuarioPerfilFactory.EnsureSecurity(profile);
            newSecurity.FechaUltimoLogin = now;
            newSecurity.UltimoAcceso = now;
            newSecurity.IpUltimoLogin = TrimToMax(ipAddress, 45);
            newSecurity.UserAgentUltimoLogin = TrimToMax(userAgent, 500);
            dbContext.UsuariosPerfil.Add(profile);

            await auditoriaService.RegistrarAsync(
                new CurrentUserContext(user.Id, []),
                new AuditoriaRegistro(
                    null,
                    "Usuarios",
                    "CrearPerfilAutomatico",
                    nameof(UsuarioPerfil),
                    user.Id,
                    $"Perfil de usuario creado automáticamente al iniciar sesión: {user.Email}",
                    ValoresNuevos: ToProfileAuditSnapshot(profile),
                    Metadata: new { Origen = "Login" }),
                cancellationToken);
            return;
        }

        var security = UsuarioPerfilFactory.EnsureSecurity(profile);
        security.FechaUltimoLogin = now;
        security.UltimoAcceso = now;
        security.IpUltimoLogin = TrimToMax(ipAddress, 45);
        security.UserAgentUltimoLogin = TrimToMax(userAgent, 500);
        security.FechaActualizacion = now;
    }

    private string CreateToken(IdentityUser user, IEnumerable<string> roles, DateTime expiresAt)
    {
        var options = jwtOptions.Value;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        await roleManager.CreateAsync(new IdentityRole(roleName));
    }

    private static AuthResult InvalidCredentials() => AuthResult.Failure(["Correo o contraseña inválidos."]);

    private static AuthResult InvalidRefreshToken() => AuthResult.Failure(["Refresh token inválido o expirado."]);

    private static AuthResult ValidationFailure(IEnumerable<ValidationError> errors)
    {
        return AuthResult.Failure(errors.Select(error =>
            string.IsNullOrWhiteSpace(error.Field)
                ? error.Message
                : $"{error.Field}: {error.Message}"));
    }

    private async Task<bool> ComunaIsValidAsync(int? idComuna, CancellationToken cancellationToken)
    {
        if (!idComuna.HasValue)
        {
            return true;
        }

        return await dbContext.Comunas.AnyAsync(
            item => item.IdComuna == idComuna.Value &&
                item.Activo &&
                item.Ciudad.Activo &&
                item.Ciudad.Pais.Activo,
            cancellationToken);
    }

    private async Task<bool> UserIsActiveAsync(string userId, CancellationToken cancellationToken)
    {
        return !await dbContext.UsuariosPerfil
            .AsNoTracking()
            .AnyAsync(profile => profile.UserId == userId && !profile.Activo, cancellationToken);
    }

    private static string? TrimToMax(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static object ToProfileAuditSnapshot(UsuarioPerfil profile)
    {
        return new
        {
            profile.UserId,
            profile.Nombre,
            profile.Apellido,
            profile.NombreCompleto,
            profile.Rut,
            profile.FechaNacimiento,
            profile.AvatarUrl,
            TelefonoAlternativo = profile.Contacto?.TelefonoAlternativo,
            Direccion = profile.Direccion?.Direccion,
            IdComuna = profile.Direccion?.IdComuna,
            profile.Activo
        };
    }
}
