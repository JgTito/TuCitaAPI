using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.HorariosPrestador;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.HorariosPrestador;

public sealed class HorarioPrestadorService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : IHorarioPrestadorService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";

    public async Task<PagedResult<HorarioPrestadorDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        HorarioPrestadorQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await PrestadorExistsAsync(idNegocio, idPrestador, cancellationToken) ||
            !await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return new PagedResult<HorarioPrestadorDto>([], query.PageNumber, query.PageSize, 0);
        }

        var horariosQuery = BaseQuery(idNegocio, idPrestador).AsNoTracking();

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

        return new PagedResult<HorarioPrestadorDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<ServiceResult<HorarioPrestadorDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        int idHorarioPrestador,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, idPrestador, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var horario = await BaseQuery(idNegocio, idPrestador)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdHorarioPrestador == idHorarioPrestador, cancellationToken);

        return horario is null
            ? ServiceResult<HorarioPrestadorDto>.NotFound("El horario del prestador no existe.")
            : ServiceResult<HorarioPrestadorDto>.Success(ToDto(horario));
    }

    public async Task<ServiceResult<HorarioPrestadorDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        CreateHorarioPrestadorRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, idPrestador, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            idPrestador,
            request.DiaSemana,
            request.HoraInicio,
            request.HoraFin,
            request.Activo,
            null,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<HorarioPrestadorDto>.Validation(validationErrors);
        }

        var horario = new HorarioPrestador
        {
            IdNegocio = idNegocio,
            IdPrestador = idPrestador,
            DiaSemana = request.DiaSemana,
            HoraInicio = request.HoraInicio,
            HoraFin = request.HoraFin,
            Activo = request.Activo
        };

        dbContext.HorariosPrestador.Add(horario);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseQuery(idNegocio, idPrestador)
            .AsNoTracking()
            .FirstAsync(item => item.IdHorarioPrestador == horario.IdHorarioPrestador, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "HorariosPrestador",
                "Crear",
                nameof(HorarioPrestador),
                created.IdHorarioPrestador.ToString(),
                $"Horario de prestador creado para {created.Prestador.Nombre}: {GetDiaSemanaNombre(created.DiaSemana)} {created.HoraInicio:HH:mm}-{created.HoraFin:HH:mm}",
                ValoresNuevos: ToAuditSnapshot(created)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<HorarioPrestadorDto>.Success(ToDto(created));
    }

    public async Task<ServiceResult<HorarioPrestadorDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        int idHorarioPrestador,
        UpdateHorarioPrestadorRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, idPrestador, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var horario = await BaseQuery(idNegocio, idPrestador)
            .FirstOrDefaultAsync(item => item.IdHorarioPrestador == idHorarioPrestador, cancellationToken);

        if (horario is null)
        {
            return ServiceResult<HorarioPrestadorDto>.NotFound("El horario del prestador no existe.");
        }

        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            idPrestador,
            request.DiaSemana,
            request.HoraInicio,
            request.HoraFin,
            request.Activo,
            idHorarioPrestador,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<HorarioPrestadorDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(horario);

        horario.DiaSemana = request.DiaSemana;
        horario.HoraInicio = request.HoraInicio;
        horario.HoraFin = request.HoraFin;
        horario.Activo = request.Activo;

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio, idPrestador)
            .AsNoTracking()
            .FirstAsync(item => item.IdHorarioPrestador == idHorarioPrestador, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "HorariosPrestador",
                "Editar",
                nameof(HorarioPrestador),
                idHorarioPrestador.ToString(),
                $"Horario de prestador editado para {updated.Prestador.Nombre}: {GetDiaSemanaNombre(updated.DiaSemana)} {updated.HoraInicio:HH:mm}-{updated.HoraFin:HH:mm}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<HorarioPrestadorDto>.Success(ToDto(updated));
    }

    public async Task<ServiceResult<HorarioPrestadorDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        int idHorarioPrestador,
        bool activo,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, idPrestador, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var horario = await BaseQuery(idNegocio, idPrestador)
            .FirstOrDefaultAsync(item => item.IdHorarioPrestador == idHorarioPrestador, cancellationToken);

        if (horario is null)
        {
            return ServiceResult<HorarioPrestadorDto>.NotFound("El horario del prestador no existe.");
        }

        var validationErrors = await ValidateRequestAsync(
            idNegocio,
            idPrestador,
            horario.DiaSemana,
            horario.HoraInicio,
            horario.HoraFin,
            activo,
            idHorarioPrestador,
            cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<HorarioPrestadorDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(horario);

        horario.Activo = activo;
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio, idPrestador)
            .AsNoTracking()
            .FirstAsync(item => item.IdHorarioPrestador == idHorarioPrestador, cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "HorariosPrestador",
                activo ? "Activar" : "Desactivar",
                nameof(HorarioPrestador),
                idHorarioPrestador.ToString(),
                activo
                    ? $"Horario de prestador activado para {updated.Prestador.Nombre}: {GetDiaSemanaNombre(updated.DiaSemana)} {updated.HoraInicio:HH:mm}-{updated.HoraFin:HH:mm}"
                    : $"Horario de prestador desactivado para {updated.Prestador.Nombre}: {GetDiaSemanaNombre(updated.DiaSemana)} {updated.HoraInicio:HH:mm}-{updated.HoraFin:HH:mm}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<HorarioPrestadorDto>.Success(ToDto(updated));
    }

    public Task<ServiceResult<HorarioPrestadorDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        int idHorarioPrestador,
        CancellationToken cancellationToken)
    {
        return SetActiveAsync(currentUser, idNegocio, idPrestador, idHorarioPrestador, activo: false, cancellationToken);
    }

    private IQueryable<HorarioPrestador> BaseQuery(int idNegocio, int idPrestador)
    {
        return dbContext.HorariosPrestador
            .Include(horario => horario.Negocio)
            .Include(horario => horario.Prestador)
            .Where(horario => horario.IdNegocio == idNegocio && horario.IdPrestador == idPrestador);
    }

    private async Task<ServiceResult<HorarioPrestadorDto>?> ValidateAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPrestador,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<HorarioPrestadorDto>.NotFound("El negocio no existe.");
        }

        if (!await PrestadorExistsAsync(idNegocio, idPrestador, cancellationToken))
        {
            return ServiceResult<HorarioPrestadorDto>.NotFound("El prestador o recurso no existe en este negocio.");
        }

        if (!await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<HorarioPrestadorDto>.Forbidden("No tienes acceso para administrar horarios del prestador.");
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
        byte diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        bool activo,
        int? currentIdHorarioPrestador,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (diaSemana is < 1 or > 7)
        {
            errors.Add(new ValidationError(nameof(CreateHorarioPrestadorRequest.DiaSemana), "El dia de la semana debe estar entre 1 y 7."));
        }

        if (horaFin <= horaInicio)
        {
            errors.Add(new ValidationError(nameof(CreateHorarioPrestadorRequest.HoraFin), "La hora de fin debe ser mayor que la hora de inicio."));
        }

        if (errors.Count > 0 || !activo)
        {
            return errors;
        }

        var overlaps = await dbContext.HorariosPrestador.AnyAsync(
            horario =>
                horario.IdNegocio == idNegocio &&
                horario.IdPrestador == idPrestador &&
                horario.DiaSemana == diaSemana &&
                horario.Activo &&
                horario.HoraInicio < horaFin &&
                horario.HoraFin > horaInicio &&
                (!currentIdHorarioPrestador.HasValue ||
                    horario.IdHorarioPrestador != currentIdHorarioPrestador.Value),
            cancellationToken);

        if (overlaps)
        {
            errors.Add(new ValidationError(nameof(CreateHorarioPrestadorRequest.HoraInicio), "Ya existe un horario activo que se solapa para ese dia y prestador."));
        }

        return errors;
    }

    private static HorarioPrestadorDto ToDto(HorarioPrestador horario)
    {
        return new HorarioPrestadorDto(
            horario.IdHorarioPrestador,
            horario.IdNegocio,
            horario.Negocio.Nombre,
            horario.IdPrestador,
            horario.Prestador.Nombre,
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

    private static object ToAuditSnapshot(HorarioPrestador horario)
    {
        return new
        {
            horario.IdHorarioPrestador,
            horario.IdNegocio,
            Negocio = horario.Negocio.Nombre,
            horario.IdPrestador,
            Prestador = horario.Prestador.Nombre,
            horario.DiaSemana,
            DiaSemanaNombre = GetDiaSemanaNombre(horario.DiaSemana),
            horario.HoraInicio,
            horario.HoraFin,
            horario.Activo
        };
    }
}
