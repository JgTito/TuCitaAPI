using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.UsuariosPerfil;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.UsuariosPerfil;

public sealed class UsuarioPerfilService(
    ReservaFlowDbContext dbContext,
    UserManager<IdentityUser> userManager,
    IAuditoriaService auditoriaService) : IUsuarioPerfilService
{
    public async Task<ServiceResult<UsuarioPerfilDto>> GetMineAsync(
        CurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(currentUser);
        if (user is null)
        {
            return ServiceResult<UsuarioPerfilDto>.Forbidden("Debes iniciar sesión para ver tu perfil.");
        }

        var profile = await EnsureProfileAsync(currentUser, user, cancellationToken);
        return ServiceResult<UsuarioPerfilDto>.Success(ToDto(profile, user));
    }

    public async Task<ServiceResult<UsuarioPerfilDto>> UpdateMineAsync(
        CurrentUserContext currentUser,
        UpdateUsuarioPerfilRequest request,
        string? avatarUrl,
        CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(currentUser);
        if (user is null)
        {
            return ServiceResult<UsuarioPerfilDto>.Forbidden("Debes iniciar sesión para actualizar tu perfil.");
        }

        var profile = await EnsureProfileAsync(currentUser, user, cancellationToken);
        var validationErrors = await ValidateRequestAsync(request, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<UsuarioPerfilDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(profile);

        UsuarioPerfilFactory.ApplyEditable(
            profile,
            request.Nombre,
            request.Apellido,
            request.Rut,
            request.FechaNacimiento,
            avatarUrl ?? profile.AvatarUrl,
            request.TelefonoAlternativo,
            request.Direccion,
            request.IdComuna);

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "Usuarios",
                "ActualizarPerfil",
                nameof(UsuarioPerfil),
                profile.IdUsuarioPerfil.ToString(),
                $"Perfil de usuario actualizado: {user.Email}",
                previousSnapshot,
                ToAuditSnapshot(profile)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<UsuarioPerfilDto>.Success(ToDto(profile, user));
    }

    private async Task<IdentityUser?> GetUserAsync(CurrentUserContext currentUser)
    {
        return currentUser.IsAuthenticated
            ? await userManager.FindByIdAsync(currentUser.UserId)
            : null;
    }

    private async Task<UsuarioPerfil> EnsureProfileAsync(
        CurrentUserContext currentUser,
        IdentityUser user,
        CancellationToken cancellationToken)
    {
        var profile = await UsuarioPerfilQuery()
            .FirstOrDefaultAsync(item => item.UserId == user.Id, cancellationToken);

        if (profile is not null)
        {
            return profile;
        }

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

        dbContext.UsuariosPerfil.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "Usuarios",
                "CrearPerfilAutomatico",
                nameof(UsuarioPerfil),
                profile.IdUsuarioPerfil.ToString(),
                $"Perfil de usuario creado automáticamente: {user.Email}",
                ValoresNuevos: ToAuditSnapshot(profile),
                Metadata: new { Origen = "MiPerfil" }),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return profile;
    }

    private IQueryable<UsuarioPerfil> UsuarioPerfilQuery()
    {
        return dbContext.UsuariosPerfil
            .Include(item => item.Contacto)
            .Include(item => item.Direccion)
                .ThenInclude(item => item!.Comuna)
                    .ThenInclude(item => item!.Ciudad)
                        .ThenInclude(item => item!.Pais);
    }

    private async Task<List<ValidationError>> ValidateRequestAsync(
        UpdateUsuarioPerfilRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

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
                errors.Add(new ValidationError(nameof(UpdateUsuarioPerfilRequest.IdComuna), "La comuna indicada no existe o no está activa."));
            }
        }

        return errors;
    }

    private static UsuarioPerfilDto ToDto(UsuarioPerfil profile, IdentityUser user)
    {
        var direccion = profile.Direccion;
        var comuna = direccion?.Comuna;
        var ciudad = comuna?.Ciudad;
        var pais = ciudad?.Pais;

        return new UsuarioPerfilDto(
            profile.IdUsuarioPerfil,
            profile.UserId,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            profile.Nombre,
            profile.Apellido,
            profile.NombreCompleto,
            profile.Rut,
            profile.FechaNacimiento,
            profile.AvatarUrl,
            profile.Contacto?.TelefonoAlternativo,
            direccion?.Direccion,
            pais?.IdPais,
            pais?.Nombre,
            ciudad?.IdCiudad,
            ciudad?.Nombre,
            comuna?.IdComuna,
            comuna?.Nombre);
    }

    private static object ToAuditSnapshot(UsuarioPerfil profile)
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
