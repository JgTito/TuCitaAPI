using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.ReglasReserva;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.ReglasReserva;

public sealed class ReglaReservaService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : IReglaReservaService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";

    public async Task<ServiceResult<ReglaReservaDto>> GetByNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var regla = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return regla is null
            ? ServiceResult<ReglaReservaDto>.NotFound("Las reglas de reserva del negocio no existen.")
            : ServiceResult<ReglaReservaDto>.Success(ToDto(regla));
    }

    public async Task<ServiceResult<ReglaReservaDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateReglaReservaRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var exists = await dbContext.ReglasReserva.AnyAsync(regla => regla.IdNegocio == idNegocio, cancellationToken);
        if (exists)
        {
            return ServiceResult<ReglaReservaDto>.Validation([
                new ValidationError(string.Empty, "El negocio ya tiene reglas de reserva configuradas.")
            ]);
        }

        var validationErrors = ValidateRequest(
            request.MinHorasAnticipacion,
            request.MaxDiasAdelanto,
            request.HorasLimiteCancelacion,
            request.MaxCitasActivasPorCliente);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<ReglaReservaDto>.Validation(validationErrors);
        }

        var regla = new ReglaReserva
        {
            IdNegocio = idNegocio,
            MinHorasAnticipacion = request.MinHorasAnticipacion,
            MaxDiasAdelanto = request.MaxDiasAdelanto,
            PermiteCancelacionCliente = request.PermiteCancelacionCliente,
            HorasLimiteCancelacion = request.HorasLimiteCancelacion,
            RequiereConfirmacionManual = request.RequiereConfirmacionManual,
            PermiteSobreturnos = request.PermiteSobreturnos,
            MaxCitasActivasPorCliente = request.MaxCitasActivasPorCliente,
            FechaActualizacion = DateTime.Now
        };

        dbContext.ReglasReserva.Add(regla);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "ReglasReserva",
                "Crear",
                nameof(ReglaReserva),
                created.IdReglaReserva.ToString(),
                $"Reglas de reserva creadas para {created.Negocio.Nombre}",
                ValoresNuevos: ToAuditSnapshot(created)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<ReglaReservaDto>.Success(ToDto(created));
    }

    public async Task<ServiceResult<ReglaReservaDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        UpdateReglaReservaRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var regla = await BaseQuery(idNegocio).FirstOrDefaultAsync(cancellationToken);
        if (regla is null)
        {
            return ServiceResult<ReglaReservaDto>.NotFound("Las reglas de reserva del negocio no existen.");
        }

        var validationErrors = ValidateRequest(
            request.MinHorasAnticipacion,
            request.MaxDiasAdelanto,
            request.HorasLimiteCancelacion,
            request.MaxCitasActivasPorCliente);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<ReglaReservaDto>.Validation(validationErrors);
        }

        var previousSnapshot = ToAuditSnapshot(regla);

        regla.MinHorasAnticipacion = request.MinHorasAnticipacion;
        regla.MaxDiasAdelanto = request.MaxDiasAdelanto;
        regla.PermiteCancelacionCliente = request.PermiteCancelacionCliente;
        regla.HorasLimiteCancelacion = request.HorasLimiteCancelacion;
        regla.RequiereConfirmacionManual = request.RequiereConfirmacionManual;
        regla.PermiteSobreturnos = request.PermiteSobreturnos;
        regla.MaxCitasActivasPorCliente = request.MaxCitasActivasPorCliente;
        regla.FechaActualizacion = DateTime.Now;

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                idNegocio,
                "ReglasReserva",
                "Editar",
                nameof(ReglaReserva),
                updated.IdReglaReserva.ToString(),
                $"Reglas de reserva actualizadas para {updated.Negocio.Nombre}",
                previousSnapshot,
                ToAuditSnapshot(updated)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<ReglaReservaDto>.Success(ToDto(updated));
    }

    private IQueryable<ReglaReserva> BaseQuery(int idNegocio)
    {
        return dbContext.ReglasReserva
            .Include(regla => regla.Negocio)
            .Where(regla => regla.IdNegocio == idNegocio);
    }

    private async Task<ServiceResult<ReglaReservaDto>?> ValidateAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        var negocioExists = await dbContext.Negocios.AnyAsync(negocio => negocio.IdNegocio == idNegocio, cancellationToken);
        if (!negocioExists)
        {
            return ServiceResult<ReglaReservaDto>.NotFound("El negocio no existe.");
        }

        if (!await CanManageNegocioAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<ReglaReservaDto>.Forbidden("No tienes acceso para administrar reglas de reserva de este negocio.");
        }

        return null;
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

    private static List<ValidationError> ValidateRequest(
        int minHorasAnticipacion,
        int maxDiasAdelanto,
        int horasLimiteCancelacion,
        int maxCitasActivasPorCliente)
    {
        var errors = new List<ValidationError>();

        if (minHorasAnticipacion < 0)
        {
            errors.Add(new ValidationError(nameof(CreateReglaReservaRequest.MinHorasAnticipacion), "Las horas mínimas de anticipación no pueden ser negativas."));
        }

        if (maxDiasAdelanto <= 0)
        {
            errors.Add(new ValidationError(nameof(CreateReglaReservaRequest.MaxDiasAdelanto), "Los días máximos de adelanto deben ser mayores a cero."));
        }

        if (horasLimiteCancelacion < 0)
        {
            errors.Add(new ValidationError(nameof(CreateReglaReservaRequest.HorasLimiteCancelacion), "Las horas límite de cancelación no pueden ser negativas."));
        }

        if (maxCitasActivasPorCliente <= 0)
        {
            errors.Add(new ValidationError(nameof(CreateReglaReservaRequest.MaxCitasActivasPorCliente), "El máximo de citas activas por cliente debe ser mayor a cero."));
        }

        return errors;
    }

    private static ReglaReservaDto ToDto(ReglaReserva regla)
    {
        return new ReglaReservaDto(
            regla.IdReglaReserva,
            regla.IdNegocio,
            regla.Negocio.Nombre,
            regla.MinHorasAnticipacion,
            regla.MaxDiasAdelanto,
            regla.PermiteCancelacionCliente,
            regla.HorasLimiteCancelacion,
            regla.RequiereConfirmacionManual,
            regla.PermiteSobreturnos,
            regla.MaxCitasActivasPorCliente,
            regla.FechaActualizacion);
    }

    private static object ToAuditSnapshot(ReglaReserva regla)
    {
        return new
        {
            regla.IdReglaReserva,
            regla.IdNegocio,
            Negocio = regla.Negocio.Nombre,
            regla.MinHorasAnticipacion,
            regla.MaxDiasAdelanto,
            regla.PermiteCancelacionCliente,
            regla.HorasLimiteCancelacion,
            regla.RequiereConfirmacionManual,
            regla.PermiteSobreturnos,
            regla.MaxCitasActivasPorCliente,
            regla.FechaActualizacion
        };
    }
}
