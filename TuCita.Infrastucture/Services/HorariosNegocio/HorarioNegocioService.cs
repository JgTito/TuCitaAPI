using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.HorariosNegocio;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.HorariosNegocio;

public sealed class HorarioNegocioService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : IHorarioNegocioService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";

    public async Task<PagedResult<HorarioNegocioDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        HorarioNegocioQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return new PagedResult<HorarioNegocioDto>([], query.PageNumber, query.PageSize, 0);
        }

        var horariosQuery = BaseQuery(idNegocio).AsNoTracking();

        if (query.DiaSemana.HasValue)
        {
            horariosQuery = horariosQuery.Where(horario => horario.DiaSemana == query.DiaSemana.Value);
        }

        if (query.Activo.HasValue)
        {
            horariosQuery = horariosQuery.Where(horario => horario.Activo == query.Activo.Value);
        }

        var totalItems = await horariosQuery.CountAsync(cancellationToken);
        var horarios = await horariosQuery
            .OrderBy(horario => horario.DiaSemana)
            .ThenBy(horario => horario.HoraInicio)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);
        var items = horarios.Select(ToDto).ToArray();

        return new PagedResult<HorarioNegocioDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<ServiceResult<HorarioNegocioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idHorarioNegocio,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var horario = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdHorarioNegocio == idHorarioNegocio, cancellationToken);

        return horario is null
            ? ServiceResult<HorarioNegocioDto>.NotFound("El horario del negocio no existe.")
            : ServiceResult<HorarioNegocioDto>.Success(ToDto(horario));
    }

    public async Task<ServiceResult<HorarioNegocioDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateHorarioNegocioRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            request.DiaSemana,
            request.HoraInicio,
            request.HoraFin,
            request.Activo,
            null,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<HorarioNegocioDto>.Validation(validationErrors);
        }

        var horario = new HorarioNegocio
        {
            IdNegocio = idNegocio,
            DiaSemana = request.DiaSemana,
            HoraInicio = request.HoraInicio,
            HoraFin = request.HoraFin,
            Activo = request.Activo
        };

        dbContext.HorariosNegocio.Add(horario);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdHorarioNegocio == horario.IdHorarioNegocio, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "HorariosNegocio",
                "Crear",
                nameof(HorarioNegocio),
                created.IdHorarioNegocio.ToString(),
                $"Horario de negocio creado: {GetDiaSemanaNombre(created.DiaSemana)} {created.HoraInicio:HH:mm}-{created.HoraFin:HH:mm}",
                ValoresNuevos: ToAuditSnapshot(created)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<HorarioNegocioDto>.Success(ToDto(created));
    }

    public async Task<ServiceResult<HorarioNegocioDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idHorarioNegocio,
        UpdateHorarioNegocioRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var horario = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdHorarioNegocio == idHorarioNegocio, cancellationToken);

        if (horario is null)
        {
            return ServiceResult<HorarioNegocioDto>.NotFound("El horario del negocio no existe.");
        }

        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            request.DiaSemana,
            request.HoraInicio,
            request.HoraFin,
            request.Activo,
            idHorarioNegocio,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<HorarioNegocioDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(horario);

        horario.DiaSemana = request.DiaSemana;
        horario.HoraInicio = request.HoraInicio;
        horario.HoraFin = request.HoraFin;
        horario.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdHorarioNegocio == idHorarioNegocio, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "HorariosNegocio",
                "Editar",
                nameof(HorarioNegocio),
                idHorarioNegocio.ToString(),
                $"Horario de negocio editado: {GetDiaSemanaNombre(updated.DiaSemana)} {updated.HoraInicio:HH:mm}-{updated.HoraFin:HH:mm}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<HorarioNegocioDto>.Success(ToDto(updated));
    }

    public async Task<ServiceResult<HorarioNegocioDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idHorarioNegocio,
        bool activo,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var horario = await BaseQuery(idNegocio)
            .FirstOrDefaultAsync(item => item.IdHorarioNegocio == idHorarioNegocio, cancellationToken);

        if (horario is null)
        {
            return ServiceResult<HorarioNegocioDto>.NotFound("El horario del negocio no existe.");
        }

        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            horario.DiaSemana,
            horario.HoraInicio,
            horario.HoraFin,
            activo,
            idHorarioNegocio,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<HorarioNegocioDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(horario);

        horario.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdHorarioNegocio == idHorarioNegocio, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "HorariosNegocio",
                activo ? "Activar" : "Desactivar",
                nameof(HorarioNegocio),
                idHorarioNegocio.ToString(),
                activo
                    ? $"Horario de negocio activado: {GetDiaSemanaNombre(updated.DiaSemana)} {updated.HoraInicio:HH:mm}-{updated.HoraFin:HH:mm}"
                    : $"Horario de negocio desactivado: {GetDiaSemanaNombre(updated.DiaSemana)} {updated.HoraInicio:HH:mm}-{updated.HoraFin:HH:mm}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<HorarioNegocioDto>.Success(ToDto(updated));
    }

    public Task<ServiceResult<HorarioNegocioDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idHorarioNegocio,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idNegocio, idHorarioNegocio, activo: false, cancellationToken);
    }

    private IQueryable<HorarioNegocio> BaseQuery(int idNegocio)
    {
        return dbContext.HorariosNegocio
            .Include(horario => horario.Negocio)
            .Where(horario => horario.IdNegocio == idNegocio);
    }

    private async Task<ServiceResult<HorarioNegocioDto>?> ValidateAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<HorarioNegocioDto>.NotFound("El negocio no existe.");
        }

        if (!await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<HorarioNegocioDto>.Forbidden("No tienes acceso para administrar horarios de este negocio.");
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
        byte diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        bool activo,
        int? currentIdHorarioNegocio,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (diaSemana is < 1 or > 7)
        {
            errors.Add(new ValidationError(nameof(CreateHorarioNegocioRequest.DiaSemana), "El dia de la semana debe estar entre 1 y 7."));
        }

        if (horaFin <= horaInicio)
        {
            errors.Add(new ValidationError(nameof(CreateHorarioNegocioRequest.HoraFin), "La hora de fin debe ser mayor que la hora de inicio."));
        }

        if (errors.Count > 0 || !activo)
        {
            return errors;
        }

        var overlaps = await dbContext.HorariosNegocio.AnyAsync(
            horario =>
                horario.IdNegocio == idNegocio &&
                horario.DiaSemana == diaSemana &&
                horario.Activo &&
                horario.HoraInicio < horaFin &&
                horario.HoraFin > horaInicio &&
                (!currentIdHorarioNegocio.HasValue ||
                    horario.IdHorarioNegocio != currentIdHorarioNegocio.Value),
            cancellationToken);

        if (overlaps)
        {
            errors.Add(new ValidationError(nameof(CreateHorarioNegocioRequest.HoraInicio), "Ya existe un horario activo que se solapa para ese dia."));
        }

        return errors;
    }

    private static HorarioNegocioDto ToDto(HorarioNegocio horario)
    {
        return new HorarioNegocioDto(
            horario.IdHorarioNegocio,
            horario.IdNegocio,
            horario.Negocio.Nombre,
            horario.DiaSemana,
            GetDiaSemanaNombre(horario.DiaSemana),
            horario.HoraInicio,
            horario.HoraFin,
            horario.Activo);
    }

    private static string GetDiaSemanaNombre(byte diaSemana)
    {
        return diaSemana switch
        {
            1 => "Lunes",
            2 => "Martes",
            3 => "Miercoles",
            4 => "Jueves",
            5 => "Viernes",
            6 => "Sabado",
            7 => "Domingo",
            _ => "Desconocido"
        };
    }

    private static object ToAuditSnapshot(HorarioNegocio horario)
    {
        return new
        {
            horario.IdHorarioNegocio,
            horario.IdNegocio,
            Negocio = horario.Negocio.Nombre,
            horario.DiaSemana,
            DiaSemanaNombre = GetDiaSemanaNombre(horario.DiaSemana),
            horario.HoraInicio,
            horario.HoraFin,
            horario.Activo
        };
    }
}
