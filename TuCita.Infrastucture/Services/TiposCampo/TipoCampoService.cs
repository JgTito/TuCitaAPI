using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.TiposCampo;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.TiposCampo;

public sealed class TipoCampoService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : ITipoCampoService
{
    public async Task<PagedResult<TipoCampoDto>> GetAllAsync(
        TipoCampoQuery query,
        CancellationToken cancellationToken)
    {
        var tiposQuery = dbContext.TiposCampo.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            tiposQuery = tiposQuery.Where(tipo =>
                tipo.Nombre.Contains(search) ||
                (tipo.Descripcion != null && tipo.Descripcion.Contains(search)));
        }

        if (query.Activo.HasValue)
        {
            tiposQuery = tiposQuery.Where(tipo => tipo.Activo == query.Activo.Value);
        }

        var totalItems = await tiposQuery.CountAsync(cancellationToken);
        var items = await tiposQuery
            .OrderBy(tipo => tipo.Nombre)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(tipo => new TipoCampoDto(
                tipo.IdTipoCampo,
                tipo.Nombre,
                tipo.Descripcion,
                tipo.Activo))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<TipoCampoDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<IReadOnlyCollection<TipoCampoSelectDto>> GetSelectAsync(
        TipoCampoSelectQuery query,
        CancellationToken cancellationToken)
    {
        var tiposQuery = dbContext.TiposCampo.AsNoTracking().AsQueryable();

        if (query.SoloActivos)
        {
            tiposQuery = tiposQuery.Where(tipo => tipo.Activo);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            tiposQuery = tiposQuery.Where(tipo => tipo.Nombre.Contains(search));
        }

        return await tiposQuery
            .OrderBy(tipo => tipo.Nombre)
            .Select(tipo => new TipoCampoSelectDto(
                tipo.IdTipoCampo,
                tipo.Nombre))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ServiceResult<TipoCampoDto>> GetByIdAsync(
        int idTipoCampo,
        CancellationToken cancellationToken)
    {
        var tipo = await dbContext.TiposCampo
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdTipoCampo == idTipoCampo, cancellationToken);

        return tipo is null
            ? ServiceResult<TipoCampoDto>.NotFound("El tipo de campo no existe.")
            : ServiceResult<TipoCampoDto>.Success(ToDto(tipo));
    }

    public async Task<ServiceResult<TipoCampoDto>> CreateAsync(
        CurrentUserContext currentUser,
        CreateTipoCampoRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = await ValidateNombreAsync(request.Nombre, null, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<TipoCampoDto>.Validation(validationErrors);
        }

        var tipo = new TipoCampo
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            Activo = request.Activo
        };

        dbContext.TiposCampo.Add(tipo);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                "Crear",
                nameof(TipoCampo),
                tipo.IdTipoCampo.ToString(),
                $"Tipo de campo creado: {tipo.Nombre}",
                ValoresNuevos: ToAuditSnapshot(tipo)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<TipoCampoDto>.Success(ToDto(tipo));
    }

    public async Task<ServiceResult<TipoCampoDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idTipoCampo,
        UpdateTipoCampoRequest request,
        CancellationToken cancellationToken)
    {
        var tipo = await dbContext.TiposCampo.FirstOrDefaultAsync(
            item => item.IdTipoCampo == idTipoCampo,
            cancellationToken);

        if (tipo is null)
        {
            return ServiceResult<TipoCampoDto>.NotFound("El tipo de campo no existe.");
        }

        var validationErrors = await ValidateNombreAsync(request.Nombre, idTipoCampo, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<TipoCampoDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(tipo);

        tipo.Nombre = request.Nombre.Trim();
        tipo.Descripcion = request.Descripcion?.Trim();
        tipo.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                "Editar",
                nameof(TipoCampo),
                tipo.IdTipoCampo.ToString(),
                $"Tipo de campo editado: {tipo.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(tipo)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<TipoCampoDto>.Success(ToDto(tipo));
    }

    public async Task<ServiceResult<TipoCampoDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idTipoCampo,
        bool activo,
        CancellationToken cancellationToken)
    {
        var tipo = await dbContext.TiposCampo.FirstOrDefaultAsync(
            item => item.IdTipoCampo == idTipoCampo,
            cancellationToken);

        if (tipo is null)
        {
            return ServiceResult<TipoCampoDto>.NotFound("El tipo de campo no existe.");
        }

        var previousSnapshot = ToAuditSnapshot(tipo);
        tipo.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                activo ? "Activar" : "Desactivar",
                nameof(TipoCampo),
                tipo.IdTipoCampo.ToString(),
                activo ? $"Tipo de campo activado: {tipo.Nombre}" : $"Tipo de campo desactivado: {tipo.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(tipo)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<TipoCampoDto>.Success(ToDto(tipo));
    }

    public Task<ServiceResult<TipoCampoDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idTipoCampo,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idTipoCampo, activo: false, cancellationToken);
    }

    private async Task<List<ValidationError>> ValidateNombreAsync(
        string nombre,
        int? currentIdTipoCampo,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        var trimmedName = nombre.Trim();

        var exists = await dbContext.TiposCampo.AnyAsync(
            tipo => tipo.Nombre == trimmedName && (!currentIdTipoCampo.HasValue || tipo.IdTipoCampo != currentIdTipoCampo.Value),
            cancellationToken);

        if (exists)
        {
            errors.Add(new ValidationError(nameof(CreateTipoCampoRequest.Nombre), "Ya existe un tipo de campo con ese nombre."));
        }

        return errors;
    }

    private static TipoCampoDto ToDto(TipoCampo tipo)
    {
        return new TipoCampoDto(
            tipo.IdTipoCampo,
            tipo.Nombre,
            tipo.Descripcion,
            tipo.Activo);
    }

    private static object ToAuditSnapshot(TipoCampo tipo)
    {
        return new
        {
            tipo.IdTipoCampo,
            tipo.Nombre,
            tipo.Descripcion,
            tipo.Activo
        };
    }
}
