using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.Servicios;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Servicios;

public sealed class ServicioService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : IServicioService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";
    private const string RecepcionistaRoleName = "Recepcionista";
    private const string ProfesionalRoleName = "Profesional";

    public async Task<PagedResult<ServicioDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        ServicioQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return new PagedResult<ServicioDto>([], query.PageNumber, query.PageSize, 0);
        }

        var serviciosQuery = BaseQuery(idNegocio).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            serviciosQuery = serviciosQuery.Where(servicio =>
                servicio.Nombre.Contains(search) ||
                (servicio.Descripcion != null && servicio.Descripcion.Contains(search)) ||
                (servicio.CategoriaServicio != null && servicio.CategoriaServicio.Nombre.Contains(search)));
        }

        if (query.IdCategoriaServicio.HasValue)
        {
            serviciosQuery = serviciosQuery.Where(servicio => servicio.IdCategoriaServicio == query.IdCategoriaServicio.Value);
        }

        if (query.RequiereProfesional.HasValue)
        {
            serviciosQuery = serviciosQuery.Where(servicio => servicio.RequiereProfesional == query.RequiereProfesional.Value);
        }

        if (query.RequierePagoAnticipado.HasValue)
        {
            serviciosQuery = serviciosQuery.Where(servicio => servicio.RequierePagoAnticipado == query.RequierePagoAnticipado.Value);
        }

        if (query.Activo.HasValue)
        {
            serviciosQuery = serviciosQuery.Where(servicio => servicio.Activo == query.Activo.Value);
        }

        var totalItems = await serviciosQuery.CountAsync(cancellationToken);
        var items = await serviciosQuery
            .OrderBy(servicio => servicio.Nombre)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(servicio => new ServicioDto(
                servicio.IdServicio,
                servicio.IdNegocio,
                servicio.Negocio.Nombre,
                servicio.IdCategoriaServicio,
                servicio.CategoriaServicio != null ? servicio.CategoriaServicio.Nombre : null,
                servicio.Nombre,
                servicio.Descripcion,
                servicio.DuracionMinutos,
                servicio.Precio,
                servicio.RequiereProfesional,
                servicio.RequierePagoAnticipado,
                servicio.TiempoPreparacionMinutos,
                servicio.Activo,
                servicio.FechaCreacion))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<ServicioDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<IReadOnlyCollection<ServicioSelectDto>> GetSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        ServicioSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await CanReadSelectAsync(currentUser, idNegocio, cancellationToken))
        {
            return [];
        }

        var serviciosQuery = BaseQuery(idNegocio).AsNoTracking();
        serviciosQuery = ApplySelectFilters(serviciosQuery, query);

        return await serviciosQuery
            .OrderBy(servicio => servicio.Nombre)
            .Select(servicio => new ServicioSelectDto(
                servicio.IdServicio,
                servicio.CategoriaServicio == null
                    ? servicio.Nombre
                    : servicio.Nombre + " - " + servicio.CategoriaServicio.Nombre,
                servicio.Nombre,
                servicio.IdCategoriaServicio,
                servicio.CategoriaServicio == null ? null : servicio.CategoriaServicio.Nombre,
                servicio.DuracionMinutos,
                servicio.Precio,
                servicio.RequiereProfesional,
                servicio.RequierePagoAnticipado,
                servicio.TiempoPreparacionMinutos,
                servicio.Activo))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ServiceResult<ServicioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idServicio,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var servicio = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdServicio == idServicio, cancellationToken);

        return servicio is null
            ? ServiceResult<ServicioDto>.NotFound("El servicio no existe.")
            : ServiceResult<ServicioDto>.Success(ToDto(servicio));
    }

    public async Task<ServiceResult<ServicioDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateServicioRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            request.IdCategoriaServicio,
            request.Nombre,
            request.DuracionMinutos,
            request.Precio,
            request.TiempoPreparacionMinutos,
            null,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<ServicioDto>.Validation(validationErrors);
        }

        var servicio = new Servicio
        {
            IdNegocio = idNegocio,
            IdCategoriaServicio = request.IdCategoriaServicio,
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            DuracionMinutos = request.DuracionMinutos,
            Precio = request.Precio,
            RequiereProfesional = request.RequiereProfesional,
            RequierePagoAnticipado = request.RequierePagoAnticipado,
            TiempoPreparacionMinutos = request.TiempoPreparacionMinutos,
            Activo = request.Activo
        };

        dbContext.Servicios.Add(servicio);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdServicio == servicio.IdServicio, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Servicios",
                "Crear",
                nameof(Servicio),
                created.IdServicio.ToString(),
                $"Servicio creado: {created.Nombre}",
                ValoresNuevos: ToAuditSnapshot(created)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<ServicioDto>.Success(ToDto(created));
    }

    public async Task<ServiceResult<ServicioDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idServicio,
        UpdateServicioRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var servicio = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdServicio == idServicio, cancellationToken);

        if (servicio is null)
        {
            return ServiceResult<ServicioDto>.NotFound("El servicio no existe.");
        }

        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            request.IdCategoriaServicio,
            request.Nombre,
            request.DuracionMinutos,
            request.Precio,
            request.TiempoPreparacionMinutos,
            idServicio,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<ServicioDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(servicio);

        servicio.IdCategoriaServicio = request.IdCategoriaServicio;
        servicio.Nombre = request.Nombre.Trim();
        servicio.Descripcion = request.Descripcion?.Trim();
        servicio.DuracionMinutos = request.DuracionMinutos;
        servicio.Precio = request.Precio;
        servicio.RequiereProfesional = request.RequiereProfesional;
        servicio.RequierePagoAnticipado = request.RequierePagoAnticipado;
        servicio.TiempoPreparacionMinutos = request.TiempoPreparacionMinutos;
        servicio.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdServicio == idServicio, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Servicios",
                "Editar",
                nameof(Servicio),
                idServicio.ToString(),
                $"Servicio editado: {updated.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<ServicioDto>.Success(ToDto(updated));
    }

    public async Task<ServiceResult<ServicioDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idServicio,
        bool activo,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var servicio = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdServicio == idServicio, cancellationToken);

        if (servicio is null)
        {
            return ServiceResult<ServicioDto>.NotFound("El servicio no existe.");
        }

        var previousSnapshot = ToAuditSnapshot(servicio);

        servicio.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdServicio == idServicio, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Servicios",
                activo ? "Activar" : "Desactivar",
                nameof(Servicio),
                idServicio.ToString(),
                activo
                    ? $"Servicio activado: {updated.Nombre}"
                    : $"Servicio desactivado: {updated.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<ServicioDto>.Success(ToDto(updated));
    }

    public Task<ServiceResult<ServicioDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idServicio,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idNegocio, idServicio, activo: false, cancellationToken);
    }

    private IQueryable<Servicio> BaseQuery(int idNegocio)
    {
        return dbContext.Servicios
            .Include(servicio => servicio.Negocio)
            .Include(servicio => servicio.CategoriaServicio)
            .Where(servicio => servicio.IdNegocio == idNegocio);
    }

    private static IQueryable<Servicio> ApplySelectFilters(
        IQueryable<Servicio> serviciosQuery,
        ServicioSelectQuery query)
    {
        if (query.SoloActivos)
        {
            serviciosQuery = serviciosQuery.Where(servicio => servicio.Activo);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            serviciosQuery = serviciosQuery.Where(servicio =>
                servicio.Nombre.Contains(search) ||
                (servicio.Descripcion != null && servicio.Descripcion.Contains(search)) ||
                (servicio.CategoriaServicio != null && servicio.CategoriaServicio.Nombre.Contains(search)));
        }

        if (query.IdCategoriaServicio.HasValue)
        {
            serviciosQuery = serviciosQuery.Where(servicio => servicio.IdCategoriaServicio == query.IdCategoriaServicio.Value);
        }

        if (query.IdPrestador.HasValue)
        {
            serviciosQuery = serviciosQuery.Where(servicio => servicio.PrestadorServicios.Any(relacion =>
                relacion.IdPrestador == query.IdPrestador.Value &&
                relacion.Activo));
        }

        if (query.RequiereProfesional.HasValue)
        {
            serviciosQuery = serviciosQuery.Where(servicio => servicio.RequiereProfesional == query.RequiereProfesional.Value);
        }

        if (query.RequierePagoAnticipado.HasValue)
        {
            serviciosQuery = serviciosQuery.Where(servicio => servicio.RequierePagoAnticipado == query.RequierePagoAnticipado.Value);
        }

        return serviciosQuery;
    }

    private async Task<ServiceResult<ServicioDto>?> ValidateAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<ServicioDto>.NotFound("El negocio no existe.");
        }

        if (!await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<ServicioDto>.Forbidden("No tienes acceso para administrar servicios de este negocio.");
        }

        return null;
    }

    private async Task<bool> NegocioExistsAsync(int idNegocio, CancellationToken cancellationToken)
    {
        return await dbContext.Negocios.AnyAsync(negocio => negocio.IdNegocio == idNegocio, cancellationToken);
    }

    private async Task<bool> CanManageNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsSuperAdmin)
        {
            return true;
        }

        return await dbContext.NegocioUsuarios.AnyAsync(
            item =>
                item.IdNegocio == idNegocio &&
                item.UserId == currentUser.UserId &&
                item.Activo &&
                (item.RolNegocio.Nombre == OwnerRoleName || item.RolNegocio.Nombre == AdminRoleName),
            cancellationToken);
    }

    private async Task<bool> CanReadSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsSuperAdmin)
        {
            return true;
        }

        return await dbContext.NegocioUsuarios.AnyAsync(
            item =>
                item.IdNegocio == idNegocio &&
                item.UserId == currentUser.UserId &&
                item.Activo &&
                (item.RolNegocio.Nombre == OwnerRoleName ||
                    item.RolNegocio.Nombre == AdminRoleName ||
                    item.RolNegocio.Nombre == RecepcionistaRoleName ||
                    item.RolNegocio.Nombre == ProfesionalRoleName),
            cancellationToken);
    }

    private async Task<List<ValidationError>> ValidateRequestAsync(
        int idNegocio,
        int? idCategoriaServicio,
        string nombre,
        int duracionMinutos,
        decimal precio,
        int tiempoPreparacionMinutos,
        int? currentIdServicio,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        var trimmedName = nombre.Trim();

        if (idCategoriaServicio.HasValue)
        {
            var categoriaExists = await dbContext.CategoriasServicio.AnyAsync(
                categoria =>
                    categoria.IdNegocio == idNegocio &&
                    categoria.IdCategoriaServicio == idCategoriaServicio.Value &&
                    categoria.Activo,
                cancellationToken);

            if (!categoriaExists)
            {
                errors.Add(new ValidationError(nameof(CreateServicioRequest.IdCategoriaServicio), "La categoría de servicio indicada no existe o no está activa para este negocio."));
            }
        }

        var nombreExists = await dbContext.Servicios.AnyAsync(
            servicio =>
                servicio.IdNegocio == idNegocio &&
                servicio.Nombre == trimmedName &&
                (!currentIdServicio.HasValue || servicio.IdServicio != currentIdServicio.Value),
            cancellationToken);

        if (nombreExists)
        {
            errors.Add(new ValidationError(nameof(CreateServicioRequest.Nombre), "Ya existe un servicio con ese nombre en este negocio."));
        }

        if (duracionMinutos <= 0)
        {
            errors.Add(new ValidationError(nameof(CreateServicioRequest.DuracionMinutos), "La duración del servicio debe ser mayor a cero."));
        }

        if (precio < 0)
        {
            errors.Add(new ValidationError(nameof(CreateServicioRequest.Precio), "El precio no puede ser negativo."));
        }

        if (tiempoPreparacionMinutos < 0)
        {
            errors.Add(new ValidationError(nameof(CreateServicioRequest.TiempoPreparacionMinutos), "El tiempo de preparación no puede ser negativo."));
        }

        return errors;
    }

    private static ServicioDto ToDto(Servicio servicio)
    {
        return new ServicioDto(
            servicio.IdServicio,
            servicio.IdNegocio,
            servicio.Negocio.Nombre,
            servicio.IdCategoriaServicio,
            servicio.CategoriaServicio?.Nombre,
            servicio.Nombre,
            servicio.Descripcion,
            servicio.DuracionMinutos,
            servicio.Precio,
            servicio.RequiereProfesional,
            servicio.RequierePagoAnticipado,
            servicio.TiempoPreparacionMinutos,
            servicio.Activo,
            servicio.FechaCreacion);
    }

    private static object ToAuditSnapshot(Servicio servicio)
    {
        return new
        {
            servicio.IdServicio,
            servicio.IdNegocio,
            servicio.IdCategoriaServicio,
            Categoria = servicio.CategoriaServicio?.Nombre,
            servicio.Nombre,
            servicio.Descripcion,
            servicio.DuracionMinutos,
            servicio.Precio,
            servicio.RequiereProfesional,
            servicio.RequierePagoAnticipado,
            servicio.TiempoPreparacionMinutos,
            servicio.Activo,
            servicio.FechaCreacion
        };
    }
}
