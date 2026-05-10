using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.RolesNegocio;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.RolesNegocio;

public sealed class RolNegocioService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : IRolNegocioService
{
    public async Task<PagedResult<RolNegocioDto>> GetAllAsync(RolNegocioQuery query, CancellationToken cancellationToken)
    {
        var rolesQuery = dbContext.RolesNegocio.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            rolesQuery = rolesQuery.Where(role =>
                role.Nombre.Contains(search) ||
                (role.Descripcion != null && role.Descripcion.Contains(search)));
        }

        if (query.Activo.HasValue)
        {
            rolesQuery = rolesQuery.Where(role => role.Activo == query.Activo.Value);
        }

        var totalItems = await rolesQuery.CountAsync(cancellationToken);
        var items = await rolesQuery
            .OrderBy(role => role.Nombre)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(role => new RolNegocioDto(
                role.IdRolNegocio,
                role.Nombre,
                role.Descripcion,
                role.Activo))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<RolNegocioDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<ServiceResult<RolNegocioDto>> GetByIdAsync(int idRolNegocio, CancellationToken cancellationToken)
    {
        var role = await dbContext.RolesNegocio
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdRolNegocio == idRolNegocio, cancellationToken);

        return role is null
            ? ServiceResult<RolNegocioDto>.NotFound("El rol de negocio no existe.")
            : ServiceResult<RolNegocioDto>.Success(ToDto(role));
    }

    public async Task<ServiceResult<RolNegocioDto>> CreateAsync(
        CurrentUserContext currentUser,
        CreateRolNegocioRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = await ValidateNombreAsync(request.Nombre, null, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<RolNegocioDto>.Validation(validationErrors);
        }

        var role = new RolNegocio
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            Activo = request.Activo
        };

        dbContext.RolesNegocio.Add(role);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                "Crear",
                nameof(RolNegocio),
                role.IdRolNegocio.ToString(),
                $"Rol de negocio creado: {role.Nombre}",
                ValoresNuevos: ToAuditSnapshot(role)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<RolNegocioDto>.Success(ToDto(role));
    }

    public async Task<ServiceResult<RolNegocioDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idRolNegocio,
        UpdateRolNegocioRequest request,
        CancellationToken cancellationToken)
    {
        var role = await dbContext.RolesNegocio.FirstOrDefaultAsync(
            item => item.IdRolNegocio == idRolNegocio,
            cancellationToken);

        if (role is null)
        {
            return ServiceResult<RolNegocioDto>.NotFound("El rol de negocio no existe.");
        }

        var validationErrors = await ValidateNombreAsync(request.Nombre, idRolNegocio, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<RolNegocioDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(role);

        role.Nombre = request.Nombre.Trim();
        role.Descripcion = request.Descripcion?.Trim();
        role.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                "Editar",
                nameof(RolNegocio),
                role.IdRolNegocio.ToString(),
                $"Rol de negocio editado: {role.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(role)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<RolNegocioDto>.Success(ToDto(role));
    }

    public async Task<ServiceResult<RolNegocioDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idRolNegocio,
        bool activo,
        CancellationToken cancellationToken)
    {
        var role = await dbContext.RolesNegocio.FirstOrDefaultAsync(
            item => item.IdRolNegocio == idRolNegocio,
            cancellationToken);

        if (role is null)
        {
            return ServiceResult<RolNegocioDto>.NotFound("El rol de negocio no existe.");
        }

        var previousSnapshot = ToAuditSnapshot(role);
        role.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                activo ? "Activar" : "Desactivar",
                nameof(RolNegocio),
                role.IdRolNegocio.ToString(),
                activo ? $"Rol de negocio activado: {role.Nombre}" : $"Rol de negocio desactivado: {role.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(role)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<RolNegocioDto>.Success(ToDto(role));
    }

    public Task<ServiceResult<RolNegocioDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idRolNegocio,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idRolNegocio, activo: false, cancellationToken);
    }

    private async Task<List<ValidationError>> ValidateNombreAsync(
        string nombre,
        int? currentIdRolNegocio,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        var trimmedName = nombre.Trim();

        var exists = await dbContext.RolesNegocio.AnyAsync(
            role => role.Nombre == trimmedName && (!currentIdRolNegocio.HasValue || role.IdRolNegocio != currentIdRolNegocio.Value),
            cancellationToken);

        if (exists)
        {
            errors.Add(new ValidationError(nameof(CreateRolNegocioRequest.Nombre), "Ya existe un rol de negocio con ese nombre."));
        }

        return errors;
    }

    private static RolNegocioDto ToDto(RolNegocio role)
    {
        return new RolNegocioDto(
            role.IdRolNegocio,
            role.Nombre,
            role.Descripcion,
            role.Activo);
    }

    private static object ToAuditSnapshot(RolNegocio role)
    {
        return new
        {
            role.IdRolNegocio,
            role.Nombre,
            role.Descripcion,
            role.Activo
        };
    }
}
