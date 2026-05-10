using Microsoft.EntityFrameworkCore;
using TuCita.Application.CanalesNotificacion;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.CanalesNotificacion;

public sealed class CanalNotificacionService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : ICanalNotificacionService
{
    public async Task<PagedResult<CanalNotificacionDto>> GetAllAsync(
        CanalNotificacionQuery query,
        CancellationToken cancellationToken)
    {
        var canalesQuery = dbContext.CanalesNotificacion.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            canalesQuery = canalesQuery.Where(canal =>
                canal.Nombre.Contains(search) ||
                (canal.Descripcion != null && canal.Descripcion.Contains(search)));
        }

        if (query.Activo.HasValue)
        {
            canalesQuery = canalesQuery.Where(canal => canal.Activo == query.Activo.Value);
        }

        var totalItems = await canalesQuery.CountAsync(cancellationToken);
        var items = await canalesQuery
            .OrderBy(canal => canal.Nombre)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(canal => new CanalNotificacionDto(
                canal.IdCanalNotificacion,
                canal.Nombre,
                canal.Descripcion,
                canal.Activo))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<CanalNotificacionDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<ServiceResult<CanalNotificacionDto>> GetByIdAsync(
        int idCanalNotificacion,
        CancellationToken cancellationToken)
    {
        var canal = await dbContext.CanalesNotificacion
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdCanalNotificacion == idCanalNotificacion, cancellationToken);

        return canal is null
            ? ServiceResult<CanalNotificacionDto>.NotFound("El canal de notificación no existe.")
            : ServiceResult<CanalNotificacionDto>.Success(ToDto(canal));
    }

    public async Task<ServiceResult<CanalNotificacionDto>> CreateAsync(
        CurrentUserContext currentUser,
        CreateCanalNotificacionRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = await ValidateNombreAsync(request.Nombre, null, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<CanalNotificacionDto>.Validation(validationErrors);
        }

        var canal = new CanalNotificacion
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            Activo = request.Activo
        };

        dbContext.CanalesNotificacion.Add(canal);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                "Crear",
                nameof(CanalNotificacion),
                canal.IdCanalNotificacion.ToString(),
                $"Canal de notificación creado: {canal.Nombre}",
                ValoresNuevos: ToAuditSnapshot(canal)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<CanalNotificacionDto>.Success(ToDto(canal));
    }

    public async Task<ServiceResult<CanalNotificacionDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idCanalNotificacion,
        UpdateCanalNotificacionRequest request,
        CancellationToken cancellationToken)
    {
        var canal = await dbContext.CanalesNotificacion.FirstOrDefaultAsync(
            item => item.IdCanalNotificacion == idCanalNotificacion,
            cancellationToken);

        if (canal is null)
        {
            return ServiceResult<CanalNotificacionDto>.NotFound("El canal de notificación no existe.");
        }

        var validationErrors = await ValidateNombreAsync(request.Nombre, idCanalNotificacion, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<CanalNotificacionDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(canal);

        canal.Nombre = request.Nombre.Trim();
        canal.Descripcion = request.Descripcion?.Trim();
        canal.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                "Editar",
                nameof(CanalNotificacion),
                canal.IdCanalNotificacion.ToString(),
                $"Canal de notificación editado: {canal.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(canal)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<CanalNotificacionDto>.Success(ToDto(canal));
    }

    public async Task<ServiceResult<CanalNotificacionDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idCanalNotificacion,
        bool activo,
        CancellationToken cancellationToken)
    {
        var canal = await dbContext.CanalesNotificacion.FirstOrDefaultAsync(
            item => item.IdCanalNotificacion == idCanalNotificacion,
            cancellationToken);

        if (canal is null)
        {
            return ServiceResult<CanalNotificacionDto>.NotFound("El canal de notificación no existe.");
        }

        var previousSnapshot = ToAuditSnapshot(canal);
        canal.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                activo ? "Activar" : "Desactivar",
                nameof(CanalNotificacion),
                canal.IdCanalNotificacion.ToString(),
                activo ? $"Canal de notificación activado: {canal.Nombre}" : $"Canal de notificación desactivado: {canal.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(canal)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<CanalNotificacionDto>.Success(ToDto(canal));
    }

    public Task<ServiceResult<CanalNotificacionDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idCanalNotificacion,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idCanalNotificacion, activo: false, cancellationToken);
    }

    private async Task<List<ValidationError>> ValidateNombreAsync(
        string nombre,
        int? currentIdCanalNotificacion,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        var trimmedName = nombre.Trim();

        var exists = await dbContext.CanalesNotificacion.AnyAsync(
            canal => canal.Nombre == trimmedName && (!currentIdCanalNotificacion.HasValue || canal.IdCanalNotificacion != currentIdCanalNotificacion.Value),
            cancellationToken);

        if (exists)
        {
            errors.Add(new ValidationError(nameof(CreateCanalNotificacionRequest.Nombre), "Ya existe un canal de notificación con ese nombre."));
        }

        return errors;
    }

    private static CanalNotificacionDto ToDto(CanalNotificacion canal)
    {
        return new CanalNotificacionDto(
            canal.IdCanalNotificacion,
            canal.Nombre,
            canal.Descripcion,
            canal.Activo);
    }

    private static object ToAuditSnapshot(CanalNotificacion canal)
    {
        return new
        {
            canal.IdCanalNotificacion,
            canal.Nombre,
            canal.Descripcion,
            canal.Activo
        };
    }
}
