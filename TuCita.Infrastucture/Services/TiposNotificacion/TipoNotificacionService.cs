using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.TiposNotificacion;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.TiposNotificacion;

public sealed class TipoNotificacionService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : ITipoNotificacionService
{
    public async Task<PagedResult<TipoNotificacionDto>> GetAllAsync(
        TipoNotificacionQuery query,
        CancellationToken cancellationToken)
    {
        var tiposQuery = dbContext.TiposNotificacion.AsNoTracking().AsQueryable();

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
            .Select(tipo => new TipoNotificacionDto(
                tipo.IdTipoNotificacion,
                tipo.Nombre,
                tipo.Descripcion,
                tipo.Activo))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<TipoNotificacionDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<ServiceResult<TipoNotificacionDto>> GetByIdAsync(
        int idTipoNotificacion,
        CancellationToken cancellationToken)
    {
        var tipo = await dbContext.TiposNotificacion
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdTipoNotificacion == idTipoNotificacion, cancellationToken);

        return tipo is null
            ? ServiceResult<TipoNotificacionDto>.NotFound("El tipo de notificación no existe.")
            : ServiceResult<TipoNotificacionDto>.Success(ToDto(tipo));
    }

    public async Task<ServiceResult<TipoNotificacionDto>> CreateAsync(
        CurrentUserContext currentUser,
        CreateTipoNotificacionRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = await ValidateNombreAsync(request.Nombre, null, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<TipoNotificacionDto>.Validation(validationErrors);
        }

        var tipo = new TipoNotificacion
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            Activo = request.Activo
        };

        dbContext.TiposNotificacion.Add(tipo);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                "Crear",
                nameof(TipoNotificacion),
                tipo.IdTipoNotificacion.ToString(),
                $"Tipo de notificación creado: {tipo.Nombre}",
                ValoresNuevos: ToAuditSnapshot(tipo)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<TipoNotificacionDto>.Success(ToDto(tipo));
    }

    public async Task<ServiceResult<TipoNotificacionDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idTipoNotificacion,
        UpdateTipoNotificacionRequest request,
        CancellationToken cancellationToken)
    {
        var tipo = await dbContext.TiposNotificacion.FirstOrDefaultAsync(
            item => item.IdTipoNotificacion == idTipoNotificacion,
            cancellationToken);

        if (tipo is null)
        {
            return ServiceResult<TipoNotificacionDto>.NotFound("El tipo de notificación no existe.");
        }

        var validationErrors = await ValidateNombreAsync(request.Nombre, idTipoNotificacion, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<TipoNotificacionDto>.Validation(validationErrors);
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
                nameof(TipoNotificacion),
                tipo.IdTipoNotificacion.ToString(),
                $"Tipo de notificación editado: {tipo.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(tipo)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<TipoNotificacionDto>.Success(ToDto(tipo));
    }

    public async Task<ServiceResult<TipoNotificacionDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idTipoNotificacion,
        bool activo,
        CancellationToken cancellationToken)
    {
        var tipo = await dbContext.TiposNotificacion.FirstOrDefaultAsync(
            item => item.IdTipoNotificacion == idTipoNotificacion,
            cancellationToken);

        if (tipo is null)
        {
            return ServiceResult<TipoNotificacionDto>.NotFound("El tipo de notificación no existe.");
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
                nameof(TipoNotificacion),
                tipo.IdTipoNotificacion.ToString(),
                activo ? $"Tipo de notificación activado: {tipo.Nombre}" : $"Tipo de notificación desactivado: {tipo.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(tipo)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<TipoNotificacionDto>.Success(ToDto(tipo));
    }

    public Task<ServiceResult<TipoNotificacionDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idTipoNotificacion,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idTipoNotificacion, activo: false, cancellationToken);
    }

    private async Task<List<ValidationError>> ValidateNombreAsync(
        string nombre,
        int? currentIdTipoNotificacion,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        var trimmedName = nombre.Trim();

        var exists = await dbContext.TiposNotificacion.AnyAsync(
            tipo => tipo.Nombre == trimmedName && (!currentIdTipoNotificacion.HasValue || tipo.IdTipoNotificacion != currentIdTipoNotificacion.Value),
            cancellationToken);

        if (exists)
        {
            errors.Add(new ValidationError(nameof(CreateTipoNotificacionRequest.Nombre), "Ya existe un tipo de notificación con ese nombre."));
        }

        return errors;
    }

    private static TipoNotificacionDto ToDto(TipoNotificacion tipo)
    {
        return new TipoNotificacionDto(
            tipo.IdTipoNotificacion,
            tipo.Nombre,
            tipo.Descripcion,
            tipo.Activo);
    }

    private static object ToAuditSnapshot(TipoNotificacion tipo)
    {
        return new
        {
            tipo.IdTipoNotificacion,
            tipo.Nombre,
            tipo.Descripcion,
            tipo.Activo
        };
    }
}
