using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.SuperAdminUsuarios;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;
using TuCita.Infrastucture.UsuariosPerfil;

namespace TuCita.Infrastucture.SuperAdminUsuarios;

public sealed class SuperAdminUsuarioService(
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : ISuperAdminUsuarioService
{
    private const string DefaultRoleName = "Cliente";
    private const string SuperAdminRoleName = "SuperAdmin";

    public async Task<ServiceResult<PagedResult<SuperAdminUsuarioDto>>> GetAllAsync(
        CurrentUserContext currentUser,
        SuperAdminUsuarioQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsSuperAdmin)
        {
            return ServiceResult<PagedResult<SuperAdminUsuarioDto>>.Forbidden("No tienes permisos para administrar usuarios globales.");
        }

        var usersQuery = dbContext.Users.AsNoTracking();
        usersQuery = ApplyFilters(usersQuery, query);

        var totalItems = await usersQuery.CountAsync(cancellationToken);
        var users = await usersQuery
            .OrderBy(user => user.Email)
            .ThenBy(user => user.UserName)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        var items = await ToDtosAsync(users, cancellationToken);
        var page = new PagedResult<SuperAdminUsuarioDto>(items, query.PageNumber, query.PageSize, totalItems);
        return ServiceResult<PagedResult<SuperAdminUsuarioDto>>.Success(page);
    }

    public async Task<ServiceResult<SuperAdminUsuarioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        string userId,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsSuperAdmin)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Forbidden("No tienes permisos para administrar usuarios globales.");
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null)
        {
            return ServiceResult<SuperAdminUsuarioDto>.NotFound("El usuario no existe.");
        }

        var dto = (await ToDtosAsync([user], cancellationToken)).Single();
        return ServiceResult<SuperAdminUsuarioDto>.Success(dto);
    }

    public async Task<ServiceResult<SuperAdminUsuarioDto>> CreateAsync(
        CurrentUserContext currentUser,
        CreateSuperAdminUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsSuperAdmin)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Forbidden("No tienes permisos para administrar usuarios globales.");
        }

        var validationErrors = DataAnnotationsValidator.Validate(request).ToList();
        ValidatePasswordConfirmation(request.Password, request.ConfirmPassword, nameof(CreateSuperAdminUsuarioRequest.ConfirmPassword), validationErrors);
        var roles = NormalizeRoles(request.Roles);
        if (roles.Count == 0)
        {
            roles.Add(DefaultRoleName);
        }

        await ValidateRolesAsync(roles, validationErrors, cancellationToken);

        var email = request.Email.Trim();
        var userName = string.IsNullOrWhiteSpace(request.UserName) ? email : request.UserName.Trim();
        await ValidateUniqueEmailAsync(email, null, validationErrors);
        await ValidateUniqueUserNameAsync(userName, null, validationErrors);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Validation(validationErrors);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var user = new IdentityUser
        {
            Email = email,
            UserName = userName,
            PhoneNumber = TrimToNull(request.PhoneNumber),
            EmailConfirmed = request.EmailConfirmed,
            LockoutEnabled = true,
            LockoutEnd = request.Activo ? null : DateTimeOffset.UtcNow.AddYears(100)
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Validation(ToValidationErrors(createResult));
        }

        var roleResult = await userManager.AddToRolesAsync(user, roles);
        if (!roleResult.Succeeded)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Validation(ToValidationErrors(roleResult));
        }

        var profile = UsuarioPerfilFactory.Create(
            user.Id,
            request.Nombre,
            request.Apellido,
            request.Rut,
            request.FechaNacimiento,
            avatarUrl: null,
            telefonoAlternativo: null,
            direccion: null,
            idComuna: null,
            aceptaTerminos: false,
            aceptaMarketing: false);
        profile.Activo = request.Activo;
        dbContext.UsuariosPerfil.Add(profile);

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "Usuarios",
                "Crear",
                nameof(IdentityUser),
                user.Id,
                $"Usuario global creado: {email}",
                ValoresNuevos: new
                {
                    user.Id,
                    Email = email,
                    UserName = userName,
                    Roles = roles,
                    request.Activo
                }),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var created = await dbContext.Users.AsNoTracking().FirstAsync(item => item.Id == user.Id, cancellationToken);
        var dto = (await ToDtosAsync([created], cancellationToken)).Single();
        return ServiceResult<SuperAdminUsuarioDto>.Success(dto);
    }

    public async Task<ServiceResult<SuperAdminUsuarioDto>> UpdateAsync(
        CurrentUserContext currentUser,
        string userId,
        UpdateSuperAdminUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsSuperAdmin)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Forbidden("No tienes permisos para administrar usuarios globales.");
        }

        var validationErrors = DataAnnotationsValidator.Validate(request).ToList();
        var email = request.Email.Trim();
        var userName = request.UserName.Trim();
        await ValidateUniqueEmailAsync(email, userId, validationErrors);
        await ValidateUniqueUserNameAsync(userName, userId, validationErrors);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Validation(validationErrors);
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult<SuperAdminUsuarioDto>.NotFound("El usuario no existe.");
        }

        var previousSnapshot = await BuildAuditSnapshotAsync(user, cancellationToken);
        var profile = await GetOrCreateProfileAsync(user.Id, cancellationToken);

        if (!request.Activo && user.Id == currentUser.UserId)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Validation([
                new ValidationError(nameof(UpdateSuperAdminUsuarioRequest.Activo), "No puedes desactivar tu propio usuario SuperAdmin.")
            ]);
        }

        user.Email = email;
        user.NormalizedEmail = userManager.NormalizeEmail(email);
        user.UserName = userName;
        user.NormalizedUserName = userManager.NormalizeName(userName);
        user.PhoneNumber = TrimToNull(request.PhoneNumber);
        user.EmailConfirmed = request.EmailConfirmed;
        user.PhoneNumberConfirmed = request.PhoneNumberConfirmed;
        user.LockoutEnabled = request.LockoutEnabled;
        user.LockoutEnd = request.Activo ? null : DateTimeOffset.UtcNow.AddYears(100);

        UsuarioPerfilFactory.ApplyEditable(
            profile,
            request.Nombre,
            request.Apellido,
            request.Rut,
            request.FechaNacimiento,
            profile.AvatarUrl,
            profile.Contacto?.TelefonoAlternativo,
            profile.Direccion?.Direccion,
            profile.Direccion?.IdComuna);
        profile.Activo = request.Activo;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Validation(ToValidationErrors(updateResult));
        }

        if (!request.Activo)
        {
            await RevokeRefreshTokensAsync(user.Id, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var updatedSnapshot = await BuildAuditSnapshotAsync(user, cancellationToken);
        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "Usuarios",
                "Actualizar",
                nameof(IdentityUser),
                user.Id,
                $"Usuario global actualizado: {email}",
                ValoresAnteriores: previousSnapshot,
                ValoresNuevos: updatedSnapshot),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await dbContext.Users.AsNoTracking().FirstAsync(item => item.Id == user.Id, cancellationToken);
        var dto = (await ToDtosAsync([updated], cancellationToken)).Single();
        return ServiceResult<SuperAdminUsuarioDto>.Success(dto);
    }

    public async Task<ServiceResult<SuperAdminUsuarioDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        string userId,
        bool activo,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsSuperAdmin)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Forbidden("No tienes permisos para administrar usuarios globales.");
        }

        if (!activo && userId == currentUser.UserId)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Validation([
                new ValidationError(nameof(userId), "No puedes desactivar tu propio usuario SuperAdmin.")
            ]);
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult<SuperAdminUsuarioDto>.NotFound("El usuario no existe.");
        }

        var previousSnapshot = await BuildAuditSnapshotAsync(user, cancellationToken);
        var profile = await GetOrCreateProfileAsync(user.Id, cancellationToken);
        profile.Activo = activo;
        profile.FechaActualizacion = DateTime.UtcNow;
        user.LockoutEnabled = true;
        user.LockoutEnd = activo ? null : DateTimeOffset.UtcNow.AddYears(100);

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Validation(ToValidationErrors(updateResult));
        }

        if (!activo)
        {
            await RevokeRefreshTokensAsync(user.Id, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var updatedSnapshot = await BuildAuditSnapshotAsync(user, cancellationToken);
        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "Usuarios",
                activo ? "Activar" : "Desactivar",
                nameof(IdentityUser),
                user.Id,
                activo ? $"Usuario global activado: {user.Email}" : $"Usuario global desactivado: {user.Email}",
                ValoresAnteriores: previousSnapshot,
                ValoresNuevos: updatedSnapshot),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await dbContext.Users.AsNoTracking().FirstAsync(item => item.Id == user.Id, cancellationToken);
        var dto = (await ToDtosAsync([updated], cancellationToken)).Single();
        return ServiceResult<SuperAdminUsuarioDto>.Success(dto);
    }

    public async Task<ServiceResult<SuperAdminUsuarioDto>> UpdateRolesAsync(
        CurrentUserContext currentUser,
        string userId,
        UpdateSuperAdminUsuarioRolesRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsSuperAdmin)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Forbidden("No tienes permisos para administrar usuarios globales.");
        }

        var roles = NormalizeRoles(request.Roles);
        var validationErrors = new List<ValidationError>();
        if (roles.Count == 0)
        {
            validationErrors.Add(new ValidationError(nameof(UpdateSuperAdminUsuarioRolesRequest.Roles), "Debes indicar al menos un rol."));
        }

        await ValidateRolesAsync(roles, validationErrors, cancellationToken);
        if (userId == currentUser.UserId && !roles.Contains(SuperAdminRoleName, StringComparer.OrdinalIgnoreCase))
        {
            validationErrors.Add(new ValidationError(nameof(UpdateSuperAdminUsuarioRolesRequest.Roles), "No puedes quitarte el rol SuperAdmin a ti mismo."));
        }

        if (validationErrors.Count > 0)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Validation(validationErrors);
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult<SuperAdminUsuarioDto>.NotFound("El usuario no existe.");
        }

        var previousSnapshot = await BuildAuditSnapshotAsync(user, cancellationToken);
        var currentRoles = await userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles
            .Where(role => !roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        var rolesToAdd = roles
            .Where(role => !currentRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return ServiceResult<SuperAdminUsuarioDto>.Validation(ToValidationErrors(removeResult));
            }
        }

        if (rolesToAdd.Length > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return ServiceResult<SuperAdminUsuarioDto>.Validation(ToValidationErrors(addResult));
            }
        }

        await RevokeRefreshTokensAsync(user.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var updatedSnapshot = await BuildAuditSnapshotAsync(user, cancellationToken);
        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "Usuarios",
                "ActualizarRoles",
                nameof(IdentityUser),
                user.Id,
                $"Roles globales actualizados para {user.Email}",
                ValoresAnteriores: previousSnapshot,
                ValoresNuevos: updatedSnapshot),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await dbContext.Users.AsNoTracking().FirstAsync(item => item.Id == user.Id, cancellationToken);
        var dto = (await ToDtosAsync([updated], cancellationToken)).Single();
        return ServiceResult<SuperAdminUsuarioDto>.Success(dto);
    }

    public async Task<ServiceResult<SuperAdminUsuarioDto>> ResetPasswordAsync(
        CurrentUserContext currentUser,
        string userId,
        ResetSuperAdminUsuarioPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsSuperAdmin)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Forbidden("No tienes permisos para administrar usuarios globales.");
        }

        var validationErrors = DataAnnotationsValidator.Validate(request).ToList();
        ValidatePasswordConfirmation(
            request.NewPassword,
            request.ConfirmPassword,
            nameof(ResetSuperAdminUsuarioPasswordRequest.ConfirmPassword),
            validationErrors);

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult<SuperAdminUsuarioDto>.NotFound("El usuario no existe.");
        }

        await ValidatePasswordAsync(user, request.NewPassword, validationErrors);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Validation(validationErrors);
        }

        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ServiceResult<SuperAdminUsuarioDto>.Validation(ToValidationErrors(updateResult));
        }

        var profile = await GetOrCreateProfileAsync(user.Id, cancellationToken);
        var security = UsuarioPerfilFactory.EnsureSecurity(profile);
        security.DebeCambiarPassword = request.ForzarCambioPassword;
        security.FechaUltimoCambioPassword = DateTime.UtcNow;
        security.FechaActualizacion = DateTime.UtcNow;
        await RevokeRefreshTokensAsync(user.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "Usuarios",
                "ResetPassword",
                nameof(IdentityUser),
                user.Id,
                $"Contraseña reiniciada para {user.Email}",
                Metadata: new { request.ForzarCambioPassword }),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await dbContext.Users.AsNoTracking().FirstAsync(item => item.Id == user.Id, cancellationToken);
        var dto = (await ToDtosAsync([updated], cancellationToken)).Single();
        return ServiceResult<SuperAdminUsuarioDto>.Success(dto);
    }

    public async Task<ServiceResult<IReadOnlyCollection<SuperAdminRolSelectDto>>> GetRolesSelectAsync(
        CurrentUserContext currentUser,
        SuperAdminRolSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsSuperAdmin)
        {
            return ServiceResult<IReadOnlyCollection<SuperAdminRolSelectDto>>.Forbidden("No tienes permisos para administrar usuarios globales.");
        }

        var rolesQuery = roleManager.Roles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rolesQuery = rolesQuery.Where(role => role.Name != null && role.Name.Contains(search));
        }

        var roles = await rolesQuery
            .OrderBy(role => role.Name)
            .Select(role => new SuperAdminRolSelectDto(
                role.Name ?? string.Empty,
                role.Name ?? string.Empty,
                dbContext.UserRoles.Count(userRole => userRole.RoleId == role.Id)))
            .ToArrayAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<SuperAdminRolSelectDto>>.Success(roles);
    }

    private IQueryable<IdentityUser> ApplyFilters(IQueryable<IdentityUser> query, SuperAdminUsuarioQuery filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(user =>
                (user.Email != null && user.Email.Contains(search)) ||
                (user.UserName != null && user.UserName.Contains(search)) ||
                (user.PhoneNumber != null && user.PhoneNumber.Contains(search)) ||
                dbContext.UsuariosPerfil.Any(profile =>
                    profile.UserId == user.Id &&
                    ((profile.Nombre != null && profile.Nombre.Contains(search)) ||
                        (profile.Apellido != null && profile.Apellido.Contains(search)) ||
                        (profile.NombreCompleto != null && profile.NombreCompleto.Contains(search)) ||
                        (profile.Rut != null && profile.Rut.Contains(search)))));
        }

        if (!string.IsNullOrWhiteSpace(filters.Rol))
        {
            var normalizedRole = roleManager.NormalizeKey(filters.Rol.Trim());
            query = query.Where(user =>
                dbContext.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id &&
                    dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.NormalizedName == normalizedRole)));
        }

        if (filters.EmailConfirmado.HasValue)
        {
            query = query.Where(user => user.EmailConfirmed == filters.EmailConfirmado.Value);
        }

        if (filters.TieneNegocios.HasValue)
        {
            query = filters.TieneNegocios.Value
                ? query.Where(user => dbContext.NegocioUsuarios.Any(item => item.UserId == user.Id))
                : query.Where(user => !dbContext.NegocioUsuarios.Any(item => item.UserId == user.Id));
        }

        if (filters.Activo.HasValue)
        {
            var now = DateTimeOffset.UtcNow;
            query = filters.Activo.Value
                ? query.Where(user =>
                    (!user.LockoutEnd.HasValue || user.LockoutEnd <= now) &&
                    !dbContext.UsuariosPerfil.Any(profile => profile.UserId == user.Id && !profile.Activo))
                : query.Where(user =>
                    (user.LockoutEnd.HasValue && user.LockoutEnd > now) ||
                    dbContext.UsuariosPerfil.Any(profile => profile.UserId == user.Id && !profile.Activo));
        }

        return query;
    }

    private async Task<IReadOnlyCollection<SuperAdminUsuarioDto>> ToDtosAsync(
        IReadOnlyCollection<IdentityUser> users,
        CancellationToken cancellationToken)
    {
        if (users.Count == 0)
        {
            return [];
        }

        var userIds = users.Select(user => user.Id).ToArray();
        var profiles = await dbContext.UsuariosPerfil
            .AsNoTracking()
            .Include(profile => profile.Contacto)
            .Include(profile => profile.Direccion)
                .ThenInclude(direccion => direccion!.Comuna)
                    .ThenInclude(comuna => comuna!.Ciudad)
                        .ThenInclude(ciudad => ciudad!.Pais)
            .Include(profile => profile.Seguridad)
            .Where(profile => userIds.Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, cancellationToken);

        var roleRows = await (
                from userRole in dbContext.UserRoles.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userIds.Contains(userRole.UserId)
                select new { userRole.UserId, role.Name })
            .ToArrayAsync(cancellationToken);
        var rolesByUser = roleRows
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => item.Name ?? string.Empty)
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .OrderBy(item => item)
                    .ToArray());

        var negociosRows = await dbContext.NegocioUsuarios
            .AsNoTracking()
            .Include(item => item.Negocio)
            .Include(item => item.RolNegocio)
            .Where(item => userIds.Contains(item.UserId))
            .OrderBy(item => item.Negocio.Nombre)
            .Select(item => new
            {
                item.UserId,
                Dto = new SuperAdminUsuarioNegocioDto(
                    item.IdNegocioUsuario,
                    item.IdNegocio,
                    item.Negocio.Nombre,
                    item.IdRolNegocio,
                    item.RolNegocio.Nombre,
                    item.Activo,
                    item.FechaCreacion)
            })
            .ToArrayAsync(cancellationToken);
        var negociosByUser = negociosRows
            .GroupBy(item => item.UserId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Dto).ToArray());

        return users
            .Select(user =>
            {
                profiles.TryGetValue(user.Id, out var profile);
                rolesByUser.TryGetValue(user.Id, out var roles);
                negociosByUser.TryGetValue(user.Id, out var negocios);

                var direccion = profile?.Direccion;
                var comuna = direccion?.Comuna;
                var ciudad = comuna?.Ciudad;
                var pais = ciudad?.Pais;

                return new SuperAdminUsuarioDto(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.UserName ?? string.Empty,
                    user.PhoneNumber,
                    user.EmailConfirmed,
                    user.PhoneNumberConfirmed,
                    user.LockoutEnabled,
                    user.LockoutEnd,
                    user.AccessFailedCount,
                    IsActive(user, profile),
                    profile?.Nombre,
                    profile?.Apellido,
                    profile?.NombreCompleto,
                    profile?.Rut,
                    profile?.FechaNacimiento,
                    profile?.AvatarUrl,
                    profile?.Contacto?.TelefonoAlternativo,
                    direccion?.Direccion,
                    pais?.IdPais,
                    pais?.Nombre,
                    ciudad?.IdCiudad,
                    ciudad?.Nombre,
                    comuna?.IdComuna,
                    comuna?.Nombre,
                    profile?.FechaCreacion,
                    profile?.FechaActualizacion,
                    profile?.Seguridad?.FechaUltimoLogin,
                    profile?.Seguridad?.UltimoAcceso,
                    roles ?? [],
                    negocios ?? []);
            })
            .ToArray();
    }

    private async Task<UsuarioPerfil> GetOrCreateProfileAsync(string userId, CancellationToken cancellationToken)
    {
        var profile = await dbContext.UsuariosPerfil
            .Include(item => item.Contacto)
            .Include(item => item.Direccion)
            .Include(item => item.Consentimiento)
            .Include(item => item.Seguridad)
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (profile is not null)
        {
            return profile;
        }

        profile = UsuarioPerfilFactory.Create(
            userId,
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
        return profile;
    }

    private async Task ValidateUniqueEmailAsync(string email, string? currentUserId, ICollection<ValidationError> errors)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null && !string.Equals(existingUser.Id, currentUserId, StringComparison.Ordinal))
        {
            errors.Add(new ValidationError(nameof(UpdateSuperAdminUsuarioRequest.Email), "Ya existe un usuario con ese correo."));
        }
    }

    private async Task ValidateUniqueUserNameAsync(string userName, string? currentUserId, ICollection<ValidationError> errors)
    {
        var existingUser = await userManager.FindByNameAsync(userName);
        if (existingUser is not null && !string.Equals(existingUser.Id, currentUserId, StringComparison.Ordinal))
        {
            errors.Add(new ValidationError(nameof(UpdateSuperAdminUsuarioRequest.UserName), "Ya existe un usuario con ese nombre de usuario."));
        }
    }

    private async Task ValidateRolesAsync(
        IReadOnlyCollection<string> roles,
        ICollection<ValidationError> errors,
        CancellationToken cancellationToken)
    {
        if (roles.Count == 0)
        {
            return;
        }

        var normalizedRoles = roles
            .Select(role => roleManager.NormalizeKey(role))
            .ToArray();
        var existingRoles = await roleManager.Roles
            .AsNoTracking()
            .Where(role => role.NormalizedName != null && normalizedRoles.Contains(role.NormalizedName))
            .Select(role => role.Name ?? string.Empty)
            .ToArrayAsync(cancellationToken);

        var missingRoles = roles
            .Where(role => !existingRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        foreach (var role in missingRoles)
        {
            errors.Add(new ValidationError(nameof(UpdateSuperAdminUsuarioRolesRequest.Roles), $"El rol global '{role}' no existe."));
        }
    }

    private async Task ValidatePasswordAsync(
        IdentityUser user,
        string password,
        ICollection<ValidationError> errors)
    {
        foreach (var validator in userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(userManager, user, password);
            if (result.Succeeded)
            {
                continue;
            }

            foreach (var error in result.Errors)
            {
                errors.Add(new ValidationError(nameof(ResetSuperAdminUsuarioPasswordRequest.NewPassword), error.Description));
            }
        }
    }

    private async Task RevokeRefreshTokensAsync(string userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var tokens = await dbContext.AuthRefreshTokens
            .Where(token =>
                token.UserId == userId &&
                !token.FechaRevocacion.HasValue &&
                token.FechaExpiracion > now)
            .ToArrayAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.FechaRevocacion = now;
        }
    }

    private async Task<object> BuildAuditSnapshotAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var profile = await dbContext.UsuariosPerfil
            .AsNoTracking()
            .Include(item => item.Contacto)
            .Include(item => item.Direccion)
            .FirstOrDefaultAsync(item => item.UserId == user.Id, cancellationToken);

        return new
        {
            user.Id,
            user.Email,
            user.UserName,
            user.PhoneNumber,
            user.EmailConfirmed,
            user.PhoneNumberConfirmed,
            user.LockoutEnabled,
            user.LockoutEnd,
            Activo = IsActive(user, profile),
            Roles = roles.OrderBy(role => role).ToArray(),
            Perfil = profile is null
                ? null
                : new
                {
                    profile.Nombre,
                    profile.Apellido,
                    profile.NombreCompleto,
                    profile.Rut,
                    profile.FechaNacimiento
                }
        };
    }

    private static void ValidatePasswordConfirmation(
        string password,
        string confirmPassword,
        string field,
        ICollection<ValidationError> errors)
    {
        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            errors.Add(new ValidationError(field, "La confirmación de contraseña no coincide."));
        }
    }

    private static List<string> NormalizeRoles(IReadOnlyCollection<string>? roles)
    {
        return roles?
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }

    private static IReadOnlyCollection<ValidationError> ToValidationErrors(IdentityResult result)
    {
        return result.Errors
            .Select(error => new ValidationError(string.Empty, error.Description))
            .ToArray();
    }

    private static bool IsActive(IdentityUser user, UsuarioPerfil? profile)
    {
        var lockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
        return !lockedOut && (profile?.Activo ?? true);
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
