using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.EstadosNotificacion;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.EstadosNotificacion;

public sealed class EstadoNotificacionService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : IEstadoNotificacionService
{
    public async Task<PagedResult<EstadoNotificacionDto>> GetAllAsync(
        EstadoNotificacionQuery query,
        CancellationToken cancellationToken)
    {
        var estadosQuery = dbContext.EstadosNotificacion.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            estadosQuery = estadosQuery.Where(estado =>
                estado.Nombre.Contains(search) ||
                (estado.Descripcion != null && estado.Descripcion.Contains(search)));
        }

        if (query.Activo.HasValue)
        {
            estadosQuery = estadosQuery.Where(estado => estado.Activo == query.Activo.Value);
        }

        var totalItems = await estadosQuery.CountAsync(cancellationToken);
        var items = await estadosQuery
            .OrderBy(estado => estado.Nombre)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(estado => new EstadoNotificacionDto(
                estado.IdEstadoNotificacion,
                estado.Nombre,
                estado.Descripcion,
                estado.Activo))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<EstadoNotificacionDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<ServiceResult<EstadoNotificacionDto>> GetByIdAsync(
        int idEstadoNotificacion,
        CancellationToken cancellationToken)
    {
        var estado = await dbContext.EstadosNotificacion
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdEstadoNotificacion == idEstadoNotificacion, cancellationToken);

        return estado is null
            ? ServiceResult<EstadoNotificacionDto>.NotFound("El estado de notificación no existe.")
            : ServiceResult<EstadoNotificacionDto>.Success(ToDto(estado));
    }

    public async Task<ServiceResult<EstadoNotificacionDto>> CreateAsync(
        CurrentUserContext currentUser,
        CreateEstadoNotificacionRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = await ValidateNombreAsync(request.Nombre, null, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<EstadoNotificacionDto>.Validation(validationErrors);
        }

        var estado = new EstadoNotificacion
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            Activo = request.Activo
        };

        dbContext.EstadosNotificacion.Add(estado);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                "Crear",
                nameof(EstadoNotificacion),
                estado.IdEstadoNotificacion.ToString(),
                $"Estado de notificación creado: {estado.Nombre}",
                ValoresNuevos: ToAuditSnapshot(estado)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<EstadoNotificacionDto>.Success(ToDto(estado));
    }

    public async Task<ServiceResult<EstadoNotificacionDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idEstadoNotificacion,
        UpdateEstadoNotificacionRequest request,
        CancellationToken cancellationToken)
    {
        var estado = await dbContext.EstadosNotificacion.FirstOrDefaultAsync(
            item => item.IdEstadoNotificacion == idEstadoNotificacion,
            cancellationToken);

        if (estado is null)
        {
            return ServiceResult<EstadoNotificacionDto>.NotFound("El estado de notificación no existe.");
        }

        var validationErrors = await ValidateNombreAsync(request.Nombre, idEstadoNotificacion, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<EstadoNotificacionDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(estado);

        estado.Nombre = request.Nombre.Trim();
        estado.Descripcion = request.Descripcion?.Trim();
        estado.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                "Editar",
                nameof(EstadoNotificacion),
                estado.IdEstadoNotificacion.ToString(),
                $"Estado de notificación editado: {estado.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(estado)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<EstadoNotificacionDto>.Success(ToDto(estado));
    }

    public async Task<ServiceResult<EstadoNotificacionDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idEstadoNotificacion,
        bool activo,
        CancellationToken cancellationToken)
    {
        var estado = await dbContext.EstadosNotificacion.FirstOrDefaultAsync(
            item => item.IdEstadoNotificacion == idEstadoNotificacion,
            cancellationToken);

        if (estado is null)
        {
            return ServiceResult<EstadoNotificacionDto>.NotFound("El estado de notificación no existe.");
        }

        var previousSnapshot = ToAuditSnapshot(estado);
        estado.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                activo ? "Activar" : "Desactivar",
                nameof(EstadoNotificacion),
                estado.IdEstadoNotificacion.ToString(),
                activo ? $"Estado de notificación activado: {estado.Nombre}" : $"Estado de notificación desactivado: {estado.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(estado)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<EstadoNotificacionDto>.Success(ToDto(estado));
    }

    public Task<ServiceResult<EstadoNotificacionDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idEstadoNotificacion,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idEstadoNotificacion, activo: false, cancellationToken);
    }

    private async Task<List<ValidationError>> ValidateNombreAsync(
        string nombre,
        int? currentIdEstadoNotificacion,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        var trimmedName = nombre.Trim();

        var exists = await dbContext.EstadosNotificacion.AnyAsync(
            estado => estado.Nombre == trimmedName && (!currentIdEstadoNotificacion.HasValue || estado.IdEstadoNotificacion != currentIdEstadoNotificacion.Value),
            cancellationToken);

        if (exists)
        {
            errors.Add(new ValidationError(nameof(CreateEstadoNotificacionRequest.Nombre), "Ya existe un estado de notificación con ese nombre."));
        }

        return errors;
    }

    private static EstadoNotificacionDto ToDto(EstadoNotificacion estado)
    {
        return new EstadoNotificacionDto(
            estado.IdEstadoNotificacion,
            estado.Nombre,
            estado.Descripcion,
            estado.Activo);
    }

    private static object ToAuditSnapshot(EstadoNotificacion estado)
    {
        return new
        {
            estado.IdEstadoNotificacion,
            estado.Nombre,
            estado.Descripcion,
            estado.Activo
        };
    }
}
