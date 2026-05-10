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
using TuCita.Application.Negocios;
using TuCita.Application.Onboarding;
using TuCita.Infrastucture.Authentication;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;
using TuCita.Infrastucture.UsuariosPerfil;

namespace TuCita.Infrastucture.Onboarding;

public sealed class OnboardingService(
    ReservaFlowDbContext dbContext,
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<JwtOptions> jwtOptions,
    IAuditoriaService auditoriaService) : IOnboardingService
{
    private const string OwnerRoleName = "Owner";

    public async Task<ServiceResult<OnboardingNegocioResponse>> RegisterDuenoNegocioAsync(
        RegisterDuenoNegocioRequest request,
        string? avatarUrl,
        CancellationToken cancellationToken)
    {
        var validationErrors = await ValidateRequestAsync(request, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<OnboardingNegocioResponse>.Validation(validationErrors);
        }

        var ownerRole = await dbContext.RolesNegocio.FirstAsync(
            role => role.Nombre == OwnerRoleName && role.Activo,
            cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var emailUsuario = request.EmailUsuario.Trim();
        var user = new IdentityUser
        {
            Email = emailUsuario,
            UserName = emailUsuario
        };

        var createUserResult = await userManager.CreateAsync(user, request.Password);
        if (!createUserResult.Succeeded)
        {
            return ServiceResult<OnboardingNegocioResponse>.Validation(
                createUserResult.Errors.Select(error => new ValidationError(string.Empty, error.Description)));
        }

        var roleResult = await EnsureAndAssignOwnerRoleAsync(user);
        if (!roleResult.Succeeded)
        {
            return ServiceResult<OnboardingNegocioResponse>.Validation(
                roleResult.Errors.Select(error => new ValidationError(string.Empty, error.Description)));
        }

        var profile = UsuarioPerfilFactory.Create(
            user.Id,
            request.NombreUsuario,
            request.ApellidoUsuario,
            request.Rut,
            request.FechaNacimiento,
            avatarUrl,
            request.TelefonoAlternativo,
            request.DireccionUsuario,
            request.IdComuna,
            request.AceptaTerminos,
            request.AceptaMarketing);
        var security = UsuarioPerfilFactory.EnsureSecurity(profile);
        security.FechaUltimoLogin = DateTime.UtcNow;
        security.UltimoAcceso = DateTime.UtcNow;
        dbContext.UsuariosPerfil.Add(profile);

        var negocio = new Negocio
        {
            IdRubro = request.IdRubro,
            Nombre = request.Nombre.Trim(),
            Slug = request.Slug.Trim(),
            Descripcion = TrimToNull(request.Descripcion),
            LogoUrl = TrimToNull(request.LogoUrl),
            Direccion = TrimToNull(request.Direccion),
            Telefono = TrimToNull(request.Telefono),
            Email = TrimToNull(request.EmailNegocio) ?? emailUsuario,
            Activo = request.Activo
        };

        dbContext.Negocios.Add(negocio);
        dbContext.ReglasReserva.Add(new ReglaReserva
        {
            Negocio = negocio
        });
        dbContext.NegocioUsuarios.Add(new NegocioUsuario
        {
            Negocio = negocio,
            UserId = user.Id,
            IdRolNegocio = ownerRole.IdRolNegocio,
            Activo = true
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await dbContext.Negocios
            .AsNoTracking()
            .Include(item => item.Rubro)
            .FirstAsync(item => item.IdNegocio == negocio.IdNegocio, cancellationToken);

        var ownerRelation = await dbContext.NegocioUsuarios
            .AsNoTracking()
            .Include(item => item.Negocio)
            .Include(item => item.RolNegocio)
            .Include(item => item.Usuario)
            .FirstAsync(
                item => item.IdNegocio == negocio.IdNegocio && item.UserId == user.Id,
                cancellationToken);

        var auditUser = new CurrentUserContext(user.Id, [OwnerRoleName]);
        await auditoriaService.RegistrarAsync(
            auditUser,
            new AuditoriaRegistro(
                created.IdNegocio,
                "Usuarios",
                "RegistrarOwner",
                nameof(IdentityUser),
                user.Id,
                $"Usuario Owner registrado desde onboarding: {emailUsuario}",
                ValoresNuevos: new
                {
                    user.Id,
                    Email = emailUsuario,
                    Roles = new[] { OwnerRoleName },
                    Perfil = ToUsuarioPerfilAuditSnapshot(profile)
                }),
            cancellationToken);

        await auditoriaService.RegistrarAsync(
            auditUser,
            new AuditoriaRegistro(
                created.IdNegocio,
                "Negocios",
                "CrearOnboarding",
                nameof(Negocio),
                created.IdNegocio.ToString(),
                $"Negocio creado desde onboarding: {created.Nombre}",
                ValoresNuevos: ToNegocioAuditSnapshot(created)),
            cancellationToken);

        await auditoriaService.RegistrarAsync(
            auditUser,
            new AuditoriaRegistro(
                created.IdNegocio,
                "UsuariosNegocio",
                "AsignarOwnerInicial",
                nameof(NegocioUsuario),
                ownerRelation.IdNegocioUsuario.ToString(),
                $"Usuario asociado como Owner inicial al negocio {created.Nombre} desde onboarding.",
                ValoresNuevos: ToNegocioUsuarioAuditSnapshot(ownerRelation)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var auth = await CreateAuthResponseAsync(user, cancellationToken, avatarUrl);
        await transaction.CommitAsync(cancellationToken);

        return ServiceResult<OnboardingNegocioResponse>.Success(new OnboardingNegocioResponse(
            auth,
            ToDto(created)));
    }

    private async Task<List<ValidationError>> ValidateRequestAsync(
        RegisterDuenoNegocioRequest request,
        CancellationToken cancellationToken)
    {
        var errors = DataAnnotationsValidator.Validate(request).ToList();
        if (errors.Count > 0)
        {
            return errors;
        }

        var emailUsuario = request.EmailUsuario.Trim();
        var slug = request.Slug.Trim();

        if (!request.AceptaTerminos)
        {
            errors.Add(new ValidationError(nameof(RegisterDuenoNegocioRequest.AceptaTerminos), "Debes aceptar los términos y condiciones para registrarte."));
        }

        var existingUser = await userManager.FindByEmailAsync(emailUsuario);
        if (existingUser is not null)
        {
            errors.Add(new ValidationError(nameof(RegisterDuenoNegocioRequest.EmailUsuario), "Ya existe un usuario registrado con ese correo."));
        }

        var rubroExists = await dbContext.Rubros.AnyAsync(
            rubro => rubro.IdRubro == request.IdRubro && rubro.Activo,
            cancellationToken);

        if (!rubroExists)
        {
            errors.Add(new ValidationError(nameof(RegisterDuenoNegocioRequest.IdRubro), "El rubro indicado no existe o no está activo."));
        }

        var slugExists = await dbContext.Negocios.AnyAsync(
            negocio => negocio.Slug == slug,
            cancellationToken);

        if (slugExists)
        {
            errors.Add(new ValidationError(nameof(RegisterDuenoNegocioRequest.Slug), "Ya existe un negocio con ese slug."));
        }

        var ownerRoleExists = await dbContext.RolesNegocio.AnyAsync(
            role => role.Nombre == OwnerRoleName && role.Activo,
            cancellationToken);

        if (!ownerRoleExists)
        {
            errors.Add(new ValidationError(string.Empty, "No existe un rol de negocio Owner activo para asociar el negocio."));
        }

        if (request.IdComuna.HasValue)
        {
            var comunaExists = await dbContext.Comunas.AnyAsync(
                item => item.IdComuna == request.IdComuna.Value &&
                    item.Activo &&
                    item.Ciudad.Activo &&
                    item.Ciudad.Pais.Activo,
                cancellationToken);

            if (!comunaExists)
            {
                errors.Add(new ValidationError(nameof(RegisterDuenoNegocioRequest.IdComuna), "La comuna indicada no existe o no está activa."));
            }
        }

        return errors;
    }

    private async Task<IdentityResult> EnsureAndAssignOwnerRoleAsync(IdentityUser user)
    {
        if (!await roleManager.RoleExistsAsync(OwnerRoleName))
        {
            var createRoleResult = await roleManager.CreateAsync(new IdentityRole(OwnerRoleName));
            if (!createRoleResult.Succeeded)
            {
                return createRoleResult;
            }
        }

        return await userManager.AddToRoleAsync(user, OwnerRoleName);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        IdentityUser user,
        CancellationToken cancellationToken,
        string? avatarUrl = null)
    {
        var roles = await userManager.GetRolesAsync(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpirationMinutes);
        var token = CreateToken(user, roles, expiresAt);
        var refreshToken = RefreshTokenGenerator.Generate();
        var refreshTokenHash = TokenHasher.Hash(refreshToken);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationDays);

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

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static NegocioDto ToDto(Negocio negocio)
    {
        return new NegocioDto(
            negocio.IdNegocio,
            negocio.IdRubro,
            negocio.Rubro.Nombre,
            negocio.Nombre,
            negocio.Slug,
            negocio.Descripcion,
            negocio.LogoUrl,
            negocio.Direccion,
            negocio.Telefono,
            negocio.Email,
            negocio.Activo,
            negocio.FechaCreacion);
    }

    private static object ToNegocioAuditSnapshot(Negocio negocio)
    {
        return new
        {
            negocio.IdNegocio,
            negocio.IdRubro,
            Rubro = negocio.Rubro.Nombre,
            negocio.Nombre,
            negocio.Slug,
            negocio.Descripcion,
            negocio.LogoUrl,
            negocio.Direccion,
            negocio.Telefono,
            negocio.Email,
            negocio.Activo,
            negocio.FechaCreacion
        };
    }

    private static object ToNegocioUsuarioAuditSnapshot(NegocioUsuario relacion)
    {
        return new
        {
            relacion.IdNegocioUsuario,
            relacion.IdNegocio,
            Negocio = relacion.Negocio.Nombre,
            relacion.UserId,
            Usuario = relacion.Usuario.Email ?? relacion.Usuario.UserName,
            relacion.IdRolNegocio,
            Rol = relacion.RolNegocio.Nombre,
            relacion.Activo,
            relacion.FechaCreacion
        };
    }

    private static object ToUsuarioPerfilAuditSnapshot(UsuarioPerfil profile)
    {
        return new
        {
            profile.IdUsuarioPerfil,
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
