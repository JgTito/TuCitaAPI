using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.PrestadorServicios;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.PrestadorServicios;

public sealed class PrestadorServicioService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : IPrestadorServicioService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";

    public async Task<PagedResult<PrestadorServicioDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        PrestadorServicioQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await PrestadorExistsAsync(idNegocio, idPrestador, cancellationToken) ||
            !await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return new PagedResult<PrestadorServicioDto>([], query.PageNumber, query.PageSize, 0);
        }

        var relacionesQuery = BaseQuery(idNegocio, idPrestador).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            relacionesQuery = relacionesQuery.Where(relacion =>
                relacion.Servicio.Nombre.Contains(search) ||
                (relacion.Servicio.Descripcion != null && relacion.Servicio.Descripcion.Contains(search)) ||
                (relacion.Servicio.CategoriaServicio != null && relacion.Servicio.CategoriaServicio.Nombre.Contains(search)));
        }

        if (query.IdServicio.HasValue)
        {
            relacionesQuery = relacionesQuery.Where(relacion => relacion.IdServicio == query.IdServicio.Value);
        }

        if (query.IdCategoriaServicio.HasValue)
        {
            relacionesQuery = relacionesQuery.Where(relacion => relacion.Servicio.IdCategoriaServicio == query.IdCategoriaServicio.Value);
        }

        if (query.Activo.HasValue)
        {
            relacionesQuery = relacionesQuery.Where(relacion => relacion.Activo == query.Activo.Value);
        }

        var totalItems = await relacionesQuery.CountAsync(cancellationToken);
        var items = await relacionesQuery
            .OrderBy(relacion => relacion.Servicio.Nombre)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(relacion => new PrestadorServicioDto(
                relacion.IdPrestadorServicio,
                relacion.IdNegocio,
                relacion.Negocio.Nombre,
                relacion.IdPrestador,
                relacion.Prestador.Nombre,
                relacion.IdServicio,
                relacion.Servicio.Nombre,
                relacion.Servicio.IdCategoriaServicio,
                relacion.Servicio.CategoriaServicio != null ? relacion.Servicio.CategoriaServicio.Nombre : null,
                relacion.Servicio.DuracionMinutos,
                relacion.Servicio.Precio,
                relacion.Servicio.RequiereProfesional,
                relacion.Servicio.RequierePagoAnticipado,
                relacion.Activo))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<PrestadorServicioDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<ServiceResult<PrestadorServicioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        int idPrestadorServicio,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, idPrestador, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var relacion = await BaseQuery(idNegocio, idPrestador)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdPrestadorServicio == idPrestadorServicio, cancellationToken);

        return relacion is null
            ? ServiceResult<PrestadorServicioDto>.NotFound("La asignación de servicio al prestador no existe.")
            : ServiceResult<PrestadorServicioDto>.Success(ToDto(relacion));
    }

    public async Task<ServiceResult<PrestadorServicioDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        CreatePrestadorServicioRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, idPrestador, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var validationErrors = await ValidateRequestAsync(idNegocio, idPrestador, request.IdServicio, null, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<PrestadorServicioDto>.Validation(validationErrors);
        }

        var relacion = new PrestadorServicio
        {
            IdNegocio = idNegocio,
            IdPrestador = idPrestador,
            IdServicio = request.IdServicio,
            Activo = request.Activo
        };

        dbContext.PrestadorServicios.Add(relacion);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseQuery(idNegocio, idPrestador)
            .AsNoTracking()
            .FirstAsync(item => item.IdPrestadorServicio == relacion.IdPrestadorServicio, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Prestadores",
                "AsignarServicio",
                nameof(PrestadorServicio),
                created.IdPrestadorServicio.ToString(),
                $"Servicio {created.Servicio.Nombre} asignado a {created.Prestador.Nombre}.",
                ValoresNuevos: ToAuditSnapshot(created)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<PrestadorServicioDto>.Success(ToDto(created));
    }

    public async Task<ServiceResult<PrestadorServicioDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        int idPrestadorServicio,
        UpdatePrestadorServicioRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, idPrestador, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var relacion = await BaseQuery(idNegocio, idPrestador)
            .FirstOrDefaultAsync(item => item.IdPrestadorServicio == idPrestadorServicio, cancellationToken);

        if (relacion is null)
        {
            return ServiceResult<PrestadorServicioDto>.NotFound("La asignación de servicio al prestador no existe.");
        }

        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            idPrestador,
            request.IdServicio,
            idPrestadorServicio,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<PrestadorServicioDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(relacion);

        relacion.IdServicio = request.IdServicio;
        relacion.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio, idPrestador)
            .AsNoTracking()
            .FirstAsync(item => item.IdPrestadorServicio == idPrestadorServicio, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Prestadores",
                "EditarServicioAsignado",
                nameof(PrestadorServicio),
                updated.IdPrestadorServicio.ToString(),
                $"Asignación de servicio editada para {updated.Prestador.Nombre}.",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<PrestadorServicioDto>.Success(ToDto(updated));
    }

    public async Task<ServiceResult<PrestadorServicioDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        int idPrestadorServicio,
        bool activo,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, idPrestador, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var relacion = await BaseQuery(idNegocio, idPrestador)
            .FirstOrDefaultAsync(item => item.IdPrestadorServicio == idPrestadorServicio, cancellationToken);

        if (relacion is null)
        {
            return ServiceResult<PrestadorServicioDto>.NotFound("La asignación de servicio al prestador no existe.");
        }

        var previousSnapshot = ToAuditSnapshot(relacion);
        relacion.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio, idPrestador)
            .AsNoTracking()
            .FirstAsync(item => item.IdPrestadorServicio == idPrestadorServicio, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Prestadores",
                activo ? "ActivarServicioAsignado" : "DesactivarServicioAsignado",
                nameof(PrestadorServicio),
                updated.IdPrestadorServicio.ToString(),
                activo
                    ? $"Servicio {updated.Servicio.Nombre} activado para {updated.Prestador.Nombre}."
                    : $"Servicio {updated.Servicio.Nombre} desactivado para {updated.Prestador.Nombre}.",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<PrestadorServicioDto>.Success(ToDto(updated));
    }

    public Task<ServiceResult<PrestadorServicioDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        int idPrestadorServicio,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idNegocio, idPrestador, idPrestadorServicio, activo: false, cancellationToken);
    }

    private IQueryable<PrestadorServicio> BaseQuery(int idNegocio, int idPrestador)
    {
        return dbContext.PrestadorServicios
            .Include(relacion => relacion.Negocio)
            .Include(relacion => relacion.Prestador)
            .Include(relacion => relacion.Servicio)
                .ThenInclude(servicio => servicio.CategoriaServicio)
            .Where(relacion => relacion.IdNegocio == idNegocio && relacion.IdPrestador == idPrestador);
    }

    private async Task<ServiceResult<PrestadorServicioDto>?> ValidateAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<PrestadorServicioDto>.NotFound("El negocio no existe.");
        }

        if (!await PrestadorExistsAsync(idNegocio, idPrestador, cancellationToken))
        {
            return ServiceResult<PrestadorServicioDto>.NotFound("El prestador o recurso no existe en este negocio.");
        }

        if (!await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<PrestadorServicioDto>.Forbidden("No tienes acceso para administrar servicios del prestador.");
        }

        return null;
    }

    private async Task<bool> NegocioExistsAsync(int idNegocio, CancellationToken cancellationToken)
    {
        return await dbContext.Negocios.AnyAsync(negocio => negocio.IdNegocio == idNegocio, cancellationToken);
    }

    private async Task<bool> PrestadorExistsAsync(
        int idNegocio,
        int idPrestador,
        CancellationToken cancellationToken)
    {
        return await dbContext.Prestadores.AnyAsync(
            prestador => prestador.IdNegocio == idNegocio && prestador.IdPrestador == idPrestador,
            cancellationToken);
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

    private async Task<List<ValidationError>> ValidateRequestAsync(
        int idNegocio,
        int idPrestador,
        int idServicio,
        int? currentIdPrestadorServicio,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        var servicioExists = await dbContext.Servicios.AnyAsync(
            servicio => servicio.IdNegocio == idNegocio && servicio.IdServicio == idServicio && servicio.Activo,
            cancellationToken);

        if (!servicioExists)
        {
            errors.Add(new ValidationError(nameof(CreatePrestadorServicioRequest.IdServicio), "El servicio indicado no existe o no está activo para este negocio."));
        }

        var relationExists = await dbContext.PrestadorServicios.AnyAsync(
            relacion =>
                relacion.IdNegocio == idNegocio &&
                relacion.IdPrestador == idPrestador &&
                relacion.IdServicio == idServicio &&
                (!currentIdPrestadorServicio.HasValue ||
                    relacion.IdPrestadorServicio != currentIdPrestadorServicio.Value),
            cancellationToken);

        if (relationExists)
        {
            errors.Add(new ValidationError(nameof(CreatePrestadorServicioRequest.IdServicio), "El servicio ya está asignado a este prestador o recurso."));
        }

        return errors;
    }

    private static PrestadorServicioDto ToDto(PrestadorServicio relacion)
    {
        return new PrestadorServicioDto(
            relacion.IdPrestadorServicio,
            relacion.IdNegocio,
            relacion.Negocio.Nombre,
            relacion.IdPrestador,
            relacion.Prestador.Nombre,
            relacion.IdServicio,
            relacion.Servicio.Nombre,
            relacion.Servicio.IdCategoriaServicio,
            relacion.Servicio.CategoriaServicio?.Nombre,
            relacion.Servicio.DuracionMinutos,
            relacion.Servicio.Precio,
            relacion.Servicio.RequiereProfesional,
            relacion.Servicio.RequierePagoAnticipado,
            relacion.Activo);
    }

    private static object ToAuditSnapshot(PrestadorServicio relacion)
    {
        return new
        {
            relacion.IdPrestadorServicio,
            relacion.IdNegocio,
            Negocio = relacion.Negocio?.Nombre,
            relacion.IdPrestador,
            Prestador = relacion.Prestador?.Nombre,
            relacion.IdServicio,
            Servicio = relacion.Servicio?.Nombre,
            IdCategoriaServicio = relacion.Servicio?.IdCategoriaServicio,
            CategoriaServicio = relacion.Servicio?.CategoriaServicio?.Nombre,
            relacion.Activo
        };
    }
}
