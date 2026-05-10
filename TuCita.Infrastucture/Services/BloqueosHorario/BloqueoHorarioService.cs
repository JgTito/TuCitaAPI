using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.BloqueosHorario;
using TuCita.Application.Common;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.BloqueosHorario;

public sealed class BloqueoHorarioService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : IBloqueoHorarioService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";

    public async Task<PagedResult<BloqueoHorarioDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        BloqueoHorarioQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return new PagedResult<BloqueoHorarioDto>([], query.PageNumber, query.PageSize, 0);
        }

        var bloqueosQuery = BaseQuery(idNegocio).AsNoTracking();

        if (query.IdPrestador.HasValue)
        {
            bloqueosQuery = bloqueosQuery.Where(bloqueo => bloqueo.IdPrestador == query.IdPrestador.Value);
        }

        if (query.FechaDesde.HasValue)
        {
            bloqueosQuery = bloqueosQuery.Where(bloqueo => bloqueo.FechaFin >= query.FechaDesde.Value);
        }

        if (query.FechaHasta.HasValue)
        {
            bloqueosQuery = bloqueosQuery.Where(bloqueo => bloqueo.FechaInicio <= query.FechaHasta.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            bloqueosQuery = bloqueosQuery.Where(bloqueo =>
                (bloqueo.Motivo != null && bloqueo.Motivo.Contains(search)) ||
                (bloqueo.Prestador != null && bloqueo.Prestador.Nombre.Contains(search)));
        }

        if (query.Activo.HasValue)
        {
            bloqueosQuery = bloqueosQuery.Where(bloqueo => bloqueo.Activo == query.Activo.Value);
        }

        var totalItems = await bloqueosQuery.CountAsync(cancellationToken);
        var items = await bloqueosQuery
            .OrderByDescending(bloqueo => bloqueo.FechaInicio)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(bloqueo => new BloqueoHorarioDto(
                bloqueo.IdBloqueoHorario,
                bloqueo.IdNegocio,
                bloqueo.Negocio.Nombre,
                bloqueo.IdPrestador,
                bloqueo.Prestador != null ? bloqueo.Prestador.Nombre : null,
                bloqueo.FechaInicio,
                bloqueo.FechaFin,
                bloqueo.Motivo,
                bloqueo.Activo,
                bloqueo.FechaCreacion))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<BloqueoHorarioDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<ServiceResult<BloqueoHorarioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idBloqueoHorario,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var bloqueo = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdBloqueoHorario == idBloqueoHorario, cancellationToken);

        return bloqueo is null
            ? ServiceResult<BloqueoHorarioDto>.NotFound("El bloqueo de horario no existe.")
            : ServiceResult<BloqueoHorarioDto>.Success(ToDto(bloqueo));
    }

    public async Task<ServiceResult<BloqueoHorarioDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateBloqueoHorarioRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            request.IdPrestador,
            request.FechaInicio,
            request.FechaFin,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<BloqueoHorarioDto>.Validation(validationErrors);
        }

        var bloqueo = new BloqueoHorario
        {
            IdNegocio = idNegocio,
            IdPrestador = request.IdPrestador,
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            Motivo = request.Motivo?.Trim(),
            Activo = request.Activo
        };

        dbContext.BloqueosHorario.Add(bloqueo);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdBloqueoHorario == bloqueo.IdBloqueoHorario, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Horarios",
                "CrearBloqueo",
                nameof(BloqueoHorario),
                created.IdBloqueoHorario.ToString(),
                $"Bloqueo de horario creado: {created.FechaInicio:g} - {created.FechaFin:g}",
                ValoresNuevos: ToAuditSnapshot(created)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<BloqueoHorarioDto>.Success(ToDto(created));
    }

    public async Task<ServiceResult<BloqueoHorarioDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idBloqueoHorario,
        UpdateBloqueoHorarioRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var bloqueo = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdBloqueoHorario == idBloqueoHorario, cancellationToken);

        if (bloqueo is null)
        {
            return ServiceResult<BloqueoHorarioDto>.NotFound("El bloqueo de horario no existe.");
        }

        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            request.IdPrestador,
            request.FechaInicio,
            request.FechaFin,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<BloqueoHorarioDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(bloqueo);

        bloqueo.IdPrestador = request.IdPrestador;
        bloqueo.FechaInicio = request.FechaInicio;
        bloqueo.FechaFin = request.FechaFin;
        bloqueo.Motivo = request.Motivo?.Trim();
        bloqueo.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdBloqueoHorario == idBloqueoHorario, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Horarios",
                "EditarBloqueo",
                nameof(BloqueoHorario),
                idBloqueoHorario.ToString(),
                $"Bloqueo de horario editado: {updated.FechaInicio:g} - {updated.FechaFin:g}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<BloqueoHorarioDto>.Success(ToDto(updated));
    }

    public async Task<ServiceResult<BloqueoHorarioDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idBloqueoHorario,
        bool activo,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var bloqueo = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdBloqueoHorario == idBloqueoHorario, cancellationToken);

        if (bloqueo is null)
        {
            return ServiceResult<BloqueoHorarioDto>.NotFound("El bloqueo de horario no existe.");
        }

        var previousSnapshot = ToAuditSnapshot(bloqueo);

        bloqueo.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdBloqueoHorario == idBloqueoHorario, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "Horarios",
                activo ? "ActivarBloqueo" : "DesactivarBloqueo",
                nameof(BloqueoHorario),
                idBloqueoHorario.ToString(),
                activo
                    ? $"Bloqueo de horario activado: {updated.FechaInicio:g} - {updated.FechaFin:g}"
                    : $"Bloqueo de horario desactivado: {updated.FechaInicio:g} - {updated.FechaFin:g}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<BloqueoHorarioDto>.Success(ToDto(updated));
    }

    public Task<ServiceResult<BloqueoHorarioDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idBloqueoHorario,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idNegocio, idBloqueoHorario, activo: false, cancellationToken);
    }

    private IQueryable<BloqueoHorario> BaseQuery(int idNegocio)
    {
        return dbContext.BloqueosHorario
            .Include(bloqueo => bloqueo.Negocio)
            .Include(bloqueo => bloqueo.Prestador)
            .Where(bloqueo => bloqueo.IdNegocio == idNegocio);
    }

    private async Task<ServiceResult<BloqueoHorarioDto>?> ValidateAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<BloqueoHorarioDto>.NotFound("El negocio no existe.");
        }

        if (!await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<BloqueoHorarioDto>.Forbidden("No tienes acceso para administrar bloqueos de horario de este negocio.");
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

    private async Task<List<ValidationError>> ValidateRequestAsync(
        int idNegocio,
        int? idPrestador,
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (fechaFin <= fechaInicio)
        {
            errors.Add(new ValidationError(nameof(CreateBloqueoHorarioRequest.FechaFin), "La fecha de fin debe ser mayor que la fecha de inicio."));
        }

        if (idPrestador.HasValue)
        {
            var prestadorExists = await dbContext.Prestadores.AnyAsync(
                prestador =>
                    prestador.IdNegocio == idNegocio &&
                    prestador.IdPrestador == idPrestador.Value,
                cancellationToken);

            if (!prestadorExists)
            {
                errors.Add(new ValidationError(nameof(CreateBloqueoHorarioRequest.IdPrestador), "El prestador o recurso indicado no existe en este negocio."));
            }
        }

        return errors;
    }

    private static BloqueoHorarioDto ToDto(BloqueoHorario bloqueo)
    {
        return new BloqueoHorarioDto(
            bloqueo.IdBloqueoHorario,
            bloqueo.IdNegocio,
            bloqueo.Negocio.Nombre,
            bloqueo.IdPrestador,
            bloqueo.Prestador?.Nombre,
            bloqueo.FechaInicio,
            bloqueo.FechaFin,
            bloqueo.Motivo,
            bloqueo.Activo,
            bloqueo.FechaCreacion);
    }

    private static object ToAuditSnapshot(BloqueoHorario bloqueo)
    {
        return new
        {
            bloqueo.IdBloqueoHorario,
            bloqueo.IdNegocio,
            Negocio = bloqueo.Negocio.Nombre,
            bloqueo.IdPrestador,
            Prestador = bloqueo.Prestador?.Nombre,
            bloqueo.FechaInicio,
            bloqueo.FechaFin,
            bloqueo.Motivo,
            bloqueo.Activo,
            bloqueo.FechaCreacion
        };
    }
}
