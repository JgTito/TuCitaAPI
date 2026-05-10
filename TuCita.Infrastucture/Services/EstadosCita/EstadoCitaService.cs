using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.EstadosCita;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.EstadosCita;

public sealed class EstadoCitaService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : IEstadoCitaService
{
    public async Task<PagedResult<EstadoCitaDto>> GetAllAsync(
        EstadoCitaQuery query,
        CancellationToken cancellationToken)
    {
        var estadosQuery = dbContext.EstadosCita.AsNoTracking().AsQueryable();

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

        if (query.EsEstadoFinal.HasValue)
        {
            estadosQuery = estadosQuery.Where(estado => estado.EsEstadoFinal == query.EsEstadoFinal.Value);
        }

        var totalItems = await estadosQuery.CountAsync(cancellationToken);
        var items = await estadosQuery
            .OrderBy(estado => estado.Nombre)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(estado => new EstadoCitaDto(
                estado.IdEstadoCita,
                estado.Nombre,
                estado.Descripcion,
                estado.EsEstadoFinal,
                estado.Activo))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<EstadoCitaDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<IReadOnlyCollection<EstadoCitaSelectDto>> GetSelectAsync(
        EstadoCitaSelectQuery query,
        CancellationToken cancellationToken)
    {
        var estadosQuery = dbContext.EstadosCita.AsNoTracking().AsQueryable();

        if (query.SoloActivos)
        {
            estadosQuery = estadosQuery.Where(estado => estado.Activo);
        }

        if (query.EsEstadoFinal.HasValue)
        {
            estadosQuery = estadosQuery.Where(estado => estado.EsEstadoFinal == query.EsEstadoFinal.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            estadosQuery = estadosQuery.Where(estado => estado.Nombre.Contains(search));
        }

        return await estadosQuery
            .OrderBy(estado => estado.EsEstadoFinal)
            .ThenBy(estado => estado.Nombre)
            .Select(estado => new EstadoCitaSelectDto(
                estado.IdEstadoCita,
                estado.Nombre,
                estado.EsEstadoFinal))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ServiceResult<EstadoCitaDto>> GetByIdAsync(
        int idEstadoCita,
        CancellationToken cancellationToken)
    {
        var estado = await dbContext.EstadosCita
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdEstadoCita == idEstadoCita, cancellationToken);

        return estado is null
            ? ServiceResult<EstadoCitaDto>.NotFound("El estado de cita no existe.")
            : ServiceResult<EstadoCitaDto>.Success(ToDto(estado));
    }

    public async Task<ServiceResult<EstadoCitaDto>> CreateAsync(
        CurrentUserContext currentUser,
        CreateEstadoCitaRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = await ValidateNombreAsync(request.Nombre, null, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<EstadoCitaDto>.Validation(validationErrors);
        }

        var estado = new EstadoCita
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            EsEstadoFinal = request.EsEstadoFinal,
            Activo = request.Activo
        };

        dbContext.EstadosCita.Add(estado);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                "Crear",
                nameof(EstadoCita),
                estado.IdEstadoCita.ToString(),
                $"Estado de cita creado: {estado.Nombre}",
                ValoresNuevos: ToAuditSnapshot(estado)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<EstadoCitaDto>.Success(ToDto(estado));
    }

    public async Task<ServiceResult<EstadoCitaDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idEstadoCita,
        UpdateEstadoCitaRequest request,
        CancellationToken cancellationToken)
    {
        var estado = await dbContext.EstadosCita.FirstOrDefaultAsync(
            item => item.IdEstadoCita == idEstadoCita,
            cancellationToken);

        if (estado is null)
        {
            return ServiceResult<EstadoCitaDto>.NotFound("El estado de cita no existe.");
        }

        var validationErrors = await ValidateNombreAsync(request.Nombre, idEstadoCita, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<EstadoCitaDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(estado);

        estado.Nombre = request.Nombre.Trim();
        estado.Descripcion = request.Descripcion?.Trim();
        estado.EsEstadoFinal = request.EsEstadoFinal;
        estado.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                null,
                "CatalogosGlobales",
                "Editar",
                nameof(EstadoCita),
                estado.IdEstadoCita.ToString(),
                $"Estado de cita editado: {estado.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(estado)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<EstadoCitaDto>.Success(ToDto(estado));
    }

    public async Task<ServiceResult<EstadoCitaDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idEstadoCita,
        bool activo,
        CancellationToken cancellationToken)
    {
        var estado = await dbContext.EstadosCita.FirstOrDefaultAsync(
            item => item.IdEstadoCita == idEstadoCita,
            cancellationToken);

        if (estado is null)
        {
            return ServiceResult<EstadoCitaDto>.NotFound("El estado de cita no existe.");
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
                nameof(EstadoCita),
                estado.IdEstadoCita.ToString(),
                activo ? $"Estado de cita activado: {estado.Nombre}" : $"Estado de cita desactivado: {estado.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(estado)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<EstadoCitaDto>.Success(ToDto(estado));
    }

    public Task<ServiceResult<EstadoCitaDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idEstadoCita,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idEstadoCita, activo: false, cancellationToken);
    }

    private async Task<List<ValidationError>> ValidateNombreAsync(
        string nombre,
        int? currentIdEstadoCita,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        var trimmedName = nombre.Trim();

        var exists = await dbContext.EstadosCita.AnyAsync(
            estado => estado.Nombre == trimmedName && (!currentIdEstadoCita.HasValue || estado.IdEstadoCita != currentIdEstadoCita.Value),
            cancellationToken);

        if (exists)
        {
            errors.Add(new ValidationError(nameof(CreateEstadoCitaRequest.Nombre), "Ya existe un estado de cita con ese nombre."));
        }

        return errors;
    }

    private static EstadoCitaDto ToDto(EstadoCita estado)
    {
        return new EstadoCitaDto(
            estado.IdEstadoCita,
            estado.Nombre,
            estado.Descripcion,
            estado.EsEstadoFinal,
            estado.Activo);
    }

    private static object ToAuditSnapshot(EstadoCita estado)
    {
        return new
        {
            estado.IdEstadoCita,
            estado.Nombre,
            estado.Descripcion,
            estado.EsEstadoFinal,
            estado.Activo
        };
    }
}
