using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.Rubros;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Rubros;

public sealed class RubroService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : IRubroService
{
    public async Task<PagedResult<RubroDto>> GetAllAsync(RubroQuery query, CancellationToken cancellationToken)
    {
        var rubrosQuery = dbContext.Rubros.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            rubrosQuery = rubrosQuery.Where(rubro =>
                rubro.Nombre.Contains(search) ||
                (rubro.Descripcion != null && rubro.Descripcion.Contains(search)));
        }

        if (query.Activo.HasValue)
        {
            rubrosQuery = rubrosQuery.Where(rubro => rubro.Activo == query.Activo.Value);
        }

        var totalItems = await rubrosQuery.CountAsync(cancellationToken);
        var items = await rubrosQuery
            .OrderBy(rubro => rubro.Nombre)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(rubro => new RubroDto(
                rubro.IdRubro,
                rubro.Nombre,
                rubro.Descripcion,
                rubro.Activo))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<RubroDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<IReadOnlyCollection<RubroSelectDto>> GetSelectAsync(
        RubroSelectQuery query,
        CancellationToken cancellationToken)
    {
        var rubrosQuery = dbContext.Rubros.AsNoTracking().AsQueryable();

        if (query.SoloActivos)
        {
            rubrosQuery = rubrosQuery.Where(rubro => rubro.Activo);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rubrosQuery = rubrosQuery.Where(rubro => rubro.Nombre.Contains(search));
        }

        return await rubrosQuery
            .OrderBy(rubro => rubro.Nombre)
            .Select(rubro => new RubroSelectDto(
                rubro.IdRubro,
                rubro.Nombre))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ServiceResult<RubroDto>> GetByIdAsync(int idRubro, CancellationToken cancellationToken)
    {
        var rubro = await dbContext.Rubros
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdRubro == idRubro, cancellationToken);

        return rubro is null
            ? ServiceResult<RubroDto>.NotFound("El rubro no existe.")
            : ServiceResult<RubroDto>.Success(ToDto(rubro));
    }

    public async Task<ServiceResult<RubroDto>> CreateAsync(
        CurrentUserContext currentUser,
        CreateRubroRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = await ValidateNombreAsync(request.Nombre, null, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<RubroDto>.Validation(validationErrors);
        }

        var rubro = new Rubro
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            Activo = request.Activo
        };

        dbContext.Rubros.Add(rubro);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                "Crear",
                nameof(Rubro),
                rubro.IdRubro.ToString(),
                $"Rubro creado: {rubro.Nombre}",
                ValoresNuevos: ToAuditSnapshot(rubro)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<RubroDto>.Success(ToDto(rubro));
    }

    public async Task<ServiceResult<RubroDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idRubro,
        UpdateRubroRequest request,
        CancellationToken cancellationToken)
    {
        var rubro = await dbContext.Rubros.FirstOrDefaultAsync(item => item.IdRubro == idRubro, cancellationToken);
        if (rubro is null)
        {
            return ServiceResult<RubroDto>.NotFound("El rubro no existe.");
        }

        var validationErrors = await ValidateNombreAsync(request.Nombre, idRubro, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<RubroDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(rubro);

        rubro.Nombre = request.Nombre.Trim();
        rubro.Descripcion = request.Descripcion?.Trim();
        rubro.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                "Editar",
                nameof(Rubro),
                rubro.IdRubro.ToString(),
                $"Rubro editado: {rubro.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(rubro)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<RubroDto>.Success(ToDto(rubro));
    }

    public async Task<ServiceResult<RubroDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idRubro,
        bool activo,
        CancellationToken cancellationToken)
    {
        var rubro = await dbContext.Rubros.FirstOrDefaultAsync(item => item.IdRubro == idRubro, cancellationToken);
        if (rubro is null)
        {
            return ServiceResult<RubroDto>.NotFound("El rubro no existe.");
        }

        var previousSnapshot = ToAuditSnapshot(rubro);
        rubro.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                activo ? "Activar" : "Desactivar",
                nameof(Rubro),
                rubro.IdRubro.ToString(),
                activo ? $"Rubro activado: {rubro.Nombre}" : $"Rubro desactivado: {rubro.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(rubro)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<RubroDto>.Success(ToDto(rubro));
    }

    public Task<ServiceResult<RubroDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idRubro,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idRubro, activo: false, cancellationToken);
    }

    private async Task<List<ValidationError>> ValidateNombreAsync(
        string nombre,
        int? currentIdRubro,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        var trimmedName = nombre.Trim();

        var exists = await dbContext.Rubros.AnyAsync(
            rubro => rubro.Nombre == trimmedName && (!currentIdRubro.HasValue || rubro.IdRubro != currentIdRubro.Value),
            cancellationToken);

        if (exists)
        {
            errors.Add(new ValidationError(nameof(CreateRubroRequest.Nombre), "Ya existe un rubro con ese nombre."));
        }

        return errors;
    }

    private static RubroDto ToDto(Rubro rubro)
    {
        return new RubroDto(
            rubro.IdRubro,
            rubro.Nombre,
            rubro.Descripcion,
            rubro.Activo);
    }

    private static object ToAuditSnapshot(Rubro rubro)
    {
        return new
        {
            rubro.IdRubro,
            rubro.Nombre,
            rubro.Descripcion,
            rubro.Activo
        };
    }
}
