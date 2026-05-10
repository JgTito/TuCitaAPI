using Microsoft.EntityFrameworkCore;
using TuCita.Application.Citas;
using TuCita.Application.Common;
using TuCita.Application.Disponibilidad;
using TuCita.Application.Notificaciones;
using TuCita.Application.Resenas;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Pagos;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Citas;

public sealed class CitaService(
    ReservaFlowDbContext dbContext,
    INotificacionService notificacionService,
    ICitaPagoImpactService citaPagoImpactService,
    IDisponibilidadService disponibilidadService,
    IResenaNegocioService resenaNegocioService) : ICitaService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";
    private const string RecepcionistaRoleName = "Recepcionista";
    private const string ProfesionalRoleName = "Profesional";
    private const string PendingStateName = "Pendiente";
    private const string ConfirmedStateName = "Confirmada";
    private const string PaymentPendingStateName = "Pendiente de pago";
    private const string RescheduledStateName = "Reagendada";
    private const string CancelledStateName = "Cancelada";
    private const string AttendedStateName = "Atendida";
    private const string NoShowStateName = "No asistió";

    public async Task<PagedResult<CitaDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CitaQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken) ||
            !await CanAccessAgendaAsync(currentUser, idNegocio, cancellationToken))
        {
            return new PagedResult<CitaDto>([], query.PageNumber, query.PageSize, 0);
        }

        var citasQuery = ApplyUserScope(BaseQuery(idNegocio).AsNoTracking(), currentUser);
        citasQuery = ApplyCitaFilters(citasQuery, query);

        var totalItems = await citasQuery.CountAsync(cancellationToken);
        var citas = await citasQuery
            .OrderBy(cita => cita.FechaInicio)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);
        var items = citas.Select(ToDto).ToArray();

        return new PagedResult<CitaDto>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<ServiceResult<CitaDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var cita = await ApplyUserScope(BaseQuery(idNegocio).AsNoTracking(), currentUser)
            .FirstOrDefaultAsync(item => item.IdCita == idCita, cancellationToken);

        return cita is null
            ? ServiceResult<CitaDto>.NotFound("La cita no existe.")
            : ServiceResult<CitaDto>.Success(ToDto(cita));
    }

    public async Task<ServiceResult<DisponibilidadDto>> GetDisponibilidadEdicionAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        DisponibilidadEdicionCitaQuery query,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<DisponibilidadDto>.NotFound("El negocio no existe.");
        }

        if (!await CanAccessAgendaAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<DisponibilidadDto>.Forbidden("No tienes acceso a la agenda de este negocio.");
        }

        var cita = await ApplyUserScope(BaseQuery(idNegocio).AsNoTracking(), currentUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdCita == idCita, cancellationToken);

        if (cita is null)
        {
            return ServiceResult<DisponibilidadDto>.NotFound("La cita no existe.");
        }

        if (cita.EstadoCita.EsEstadoFinal)
        {
            return ServiceResult<DisponibilidadDto>.Validation([
                new ValidationError(string.Empty, "No se puede editar una cita que ya está en estado final.")
            ]);
        }

        var canManageAgenda = await CanManageAgendaAsync(currentUser, idNegocio, cancellationToken);
        if (!canManageAgenda && query.IdServicio.HasValue && query.IdServicio.Value != cita.IdServicio)
        {
            return ServiceResult<DisponibilidadDto>.Forbidden("No tienes acceso para consultar disponibilidad de otro servicio.");
        }

        if (!canManageAgenda && query.IdPrestador.HasValue && query.IdPrestador.Value != cita.IdPrestador)
        {
            return ServiceResult<DisponibilidadDto>.Forbidden("No tienes acceso para consultar disponibilidad de otro prestador.");
        }

        var disponibilidadQuery = new DisponibilidadQuery
        {
            IdServicio = query.IdServicio ?? cita.IdServicio,
            IdPrestador = canManageAgenda ? query.IdPrestador : cita.IdPrestador,
            Fecha = query.Fecha,
            IntervaloMinutos = query.IntervaloMinutos
        };

        return await disponibilidadService.GetDisponibilidadAsync(
            idNegocio,
            disponibilidadQuery,
            idCita,
            cancellationToken);
    }

    public async Task<ServiceResult<CitaHistorialTimelineDto>> GetHistorialAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return ConvertAccessResult(accessResult);
        }

        var cita = await ApplyUserScope(BaseQuery(idNegocio).AsNoTracking(), currentUser)
            .FirstOrDefaultAsync(item => item.IdCita == idCita, cancellationToken);

        return cita is null
            ? ServiceResult<CitaHistorialTimelineDto>.NotFound("La cita no existe.")
            : ServiceResult<CitaHistorialTimelineDto>.Success(ToTimelineDto(cita, exposeInternalDetails: true, currentUser.UserId));
    }

    public async Task<ServiceResult<CitaDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateCitaRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateManagerAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return await CitaConcurrencyGuard.ExecuteWithBusinessScheduleLockAsync(
            dbContext,
            idNegocio,
            async () =>
            {
                var validationContext = await BuildValidationContextAsync(
                    idNegocio,
                    request.IdServicio,
                    request.IdPrestador,
                    request.IdEstadoCita,
                    request.FechaInicio,
                    request.FechaFin,
                    request.PrecioEstimado,
                    request.CamposValor,
                    null,
                    cancellationToken);

                var validationErrors = await ValidateCitaRequestAsync(idNegocio, request.IdCliente, validationContext, cancellationToken);
                if (validationErrors.Count > 0)
                {
                    return ServiceResult<CitaDto>.Validation(validationErrors);
                }

                var estado = validationContext.EstadoCita!;
                var cita = new Cita
                {
                    IdNegocio = idNegocio,
                    IdCliente = request.IdCliente,
                    IdServicio = request.IdServicio,
                    IdPrestador = request.IdPrestador,
                    IdEstadoCita = estado.IdEstadoCita,
                    Codigo = await GenerateCodigoAsync(idNegocio, cancellationToken),
                    FechaInicio = request.FechaInicio,
                    FechaFin = validationContext.FechaFin,
                    ComentarioCliente = request.ComentarioCliente?.Trim(),
                    NotaInterna = request.NotaInterna?.Trim(),
                    PrecioEstimado = validationContext.PrecioEstimado
                };

                dbContext.Citas.Add(cita);
                AddCampoValores(cita, request.CamposValor);
                AddHistorial(cita, null, estado.IdEstadoCita, currentUser.UserId, "Cita creada.");

                await dbContext.SaveChangesAsync(cancellationToken);
                await notificacionService.CrearPorCitaCreadaAsync(cita.IdCita, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                var created = await BaseQuery(idNegocio)
                    .AsNoTracking()
                    .FirstAsync(item => item.IdCita == cita.IdCita, cancellationToken);

                return ServiceResult<CitaDto>.Success(ToDto(created));
            },
            cancellationToken);
    }

    public async Task<ServiceResult<CitaDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        UpdateCitaRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateManagerAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return await CitaConcurrencyGuard.ExecuteWithBusinessScheduleLockAsync(
            dbContext,
            idNegocio,
            async () =>
            {
                var cita = await EditableQuery(idNegocio).FirstOrDefaultAsync(item => item.IdCita == idCita, cancellationToken);
                if (cita is null)
                {
                    return ServiceResult<CitaDto>.NotFound("La cita no existe.");
                }

                var pagoSnapshot = citaPagoImpactService.Capture(cita);
                var validationContext = await BuildValidationContextAsync(
                    idNegocio,
                    request.IdServicio,
                    request.IdPrestador,
                    cita.IdEstadoCita,
                    request.FechaInicio,
                    request.FechaFin,
                    request.PrecioEstimado,
                    request.CamposValor,
                    idCita,
                    cancellationToken);

                var validationErrors = await ValidateCitaRequestAsync(idNegocio, request.IdCliente, validationContext, cancellationToken);
                if (validationErrors.Count > 0)
                {
                    return ServiceResult<CitaDto>.Validation(validationErrors);
                }

                cita.IdCliente = request.IdCliente;
                cita.IdServicio = request.IdServicio;
                cita.Servicio = validationContext.Servicio!;
                cita.IdPrestador = request.IdPrestador;
                cita.FechaInicio = request.FechaInicio;
                cita.FechaFin = validationContext.FechaFin;
                cita.ComentarioCliente = request.ComentarioCliente?.Trim();
                cita.NotaInterna = request.NotaInterna?.Trim();
                cita.PrecioEstimado = validationContext.PrecioEstimado;
                cita.FechaActualizacion = DateTime.Now;

                ReplaceCampoValores(cita, request.CamposValor);
                AddHistorial(cita, cita.IdEstadoCita, cita.IdEstadoCita, currentUser.UserId, "Cita modificada.");
                await citaPagoImpactService.RegistrarActualizacionAsync(
                    cita,
                    pagoSnapshot,
                    currentUser.UserId,
                    "Cita modificada.",
                    cancellationToken);

                await dbContext.SaveChangesAsync(cancellationToken);
                await notificacionService.CrearPorCambioEstadoAsync(idCita, RescheduledStateName, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                var updated = await BaseQuery(idNegocio)
                    .AsNoTracking()
                    .FirstAsync(item => item.IdCita == idCita, cancellationToken);

                return ServiceResult<CitaDto>.Success(ToDto(updated));
            },
            cancellationToken);
    }

    public async Task<ServiceResult<CitaDto>> ReagendarAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        ReagendarCitaRequest request,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return await CitaConcurrencyGuard.ExecuteWithBusinessScheduleLockAsync(
            dbContext,
            idNegocio,
            async () =>
            {
                var cita = await EditableQuery(idNegocio).FirstOrDefaultAsync(item => item.IdCita == idCita, cancellationToken);
                if (cita is null)
                {
                    return ServiceResult<CitaDto>.NotFound("La cita no existe.");
                }

                var canManageAgenda = await CanManageAgendaAsync(currentUser, idNegocio, cancellationToken);
                var isAssignedProfessional = await IsAssignedProfessionalAsync(currentUser, idNegocio, cita.IdPrestador, cancellationToken);
                if (!canManageAgenda && !isAssignedProfessional)
                {
                    return ServiceResult<CitaDto>.Forbidden("No tienes acceso para mover esta cita.");
                }

                if (!canManageAgenda &&
                    request.IdPrestador.HasValue &&
                    !await IsAssignedProfessionalAsync(currentUser, idNegocio, request.IdPrestador.Value, cancellationToken))
                {
                    return ServiceResult<CitaDto>.Forbidden("No puedes mover la cita a un prestador que no pertenece a tu agenda.");
                }

                var idPrestador = request.IdPrestador ?? cita.IdPrestador;
                var validationContext = await BuildValidationContextAsync(
                    idNegocio,
                    cita.IdServicio,
                    idPrestador,
                    cita.IdEstadoCita,
                    request.FechaInicio,
                    request.FechaFin,
                    cita.PrecioEstimado,
                    cita.CamposValor.Select(campo => new CitaCampoValorRequest(campo.IdCampoReserva, campo.Valor)).ToArray(),
                    idCita,
                    cancellationToken);

                var validationErrors = await ValidateCitaRequestAsync(idNegocio, cita.IdCliente, validationContext, cancellationToken);
                if (validationErrors.Count > 0)
                {
                    return ServiceResult<CitaDto>.Validation(validationErrors);
                }

                var pagoSnapshot = citaPagoImpactService.Capture(cita);
                var estadoAnterior = cita.IdEstadoCita;
                var estadoReagendada = await dbContext.EstadosCita
                    .FirstOrDefaultAsync(estado => estado.Nombre == RescheduledStateName && estado.Activo, cancellationToken);

                cita.IdPrestador = idPrestador;
                cita.FechaInicio = request.FechaInicio;
                cita.FechaFin = validationContext.FechaFin;
                cita.FechaActualizacion = DateTime.Now;
                if (estadoReagendada is not null)
                {
                    cita.IdEstadoCita = estadoReagendada.IdEstadoCita;
                }

                AddHistorial(cita, estadoAnterior, cita.IdEstadoCita, currentUser.UserId, request.Observacion ?? "Cita reagendada.");
                await citaPagoImpactService.RegistrarReagendamientoAsync(
                    cita,
                    pagoSnapshot,
                    currentUser.UserId,
                    request.Observacion ?? "Cita reagendada.",
                    cancellationToken);

                await dbContext.SaveChangesAsync(cancellationToken);

                var updated = await BaseQuery(idNegocio)
                    .AsNoTracking()
                    .FirstAsync(item => item.IdCita == idCita, cancellationToken);

                return ServiceResult<CitaDto>.Success(ToDto(updated));
            },
            cancellationToken);
    }

    public async Task<ServiceResult<CitaDto>> CambiarEstadoAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        ChangeEstadoCitaRequest request,
        CancellationToken cancellationToken)
    {
        var estadoNuevo = await dbContext.EstadosCita
            .FirstOrDefaultAsync(estado => estado.IdEstadoCita == request.IdEstadoCita && estado.Activo, cancellationToken);

        if (estadoNuevo is null)
        {
            return ServiceResult<CitaDto>.Validation([
                new ValidationError(nameof(ChangeEstadoCitaRequest.IdEstadoCita), "El estado de cita indicado no existe o no está activo.")
            ]);
        }

        return await CambiarEstadoInternalAsync(
            currentUser,
            idNegocio,
            idCita,
            estadoNuevo,
            request.Observacion,
            cancellationToken);
    }

    public Task<ServiceResult<CitaDto>> ConfirmarAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        return CambiarEstadoPorNombreAsync(currentUser, idNegocio, idCita, ConfirmedStateName, request.Observacion, cancellationToken);
    }

    public Task<ServiceResult<CitaDto>> CancelarAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        return CambiarEstadoPorNombreAsync(currentUser, idNegocio, idCita, CancelledStateName, request.Observacion, cancellationToken);
    }

    public Task<ServiceResult<CitaDto>> MarcarAtendidaAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        return CambiarEstadoPorNombreAsync(currentUser, idNegocio, idCita, AttendedStateName, request.Observacion, cancellationToken);
    }

    public Task<ServiceResult<CitaDto>> MarcarNoAsistioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        return CambiarEstadoPorNombreAsync(currentUser, idNegocio, idCita, NoShowStateName, request.Observacion, cancellationToken);
    }

    public async Task<PagedResult<CitaDto>> GetMisCitasAsync(
        CurrentUserContext currentUser,
        CitaQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return new PagedResult<CitaDto>([], query.PageNumber, query.PageSize, 0);
        }

        var citasQuery = ApplyMisCitasScope(BaseQuery().AsNoTracking(), currentUser);

        citasQuery = ApplyCitaFilters(citasQuery, query);

        var totalItems = await citasQuery.CountAsync(cancellationToken);
        var citas = await citasQuery
            .OrderBy(cita => cita.FechaInicio)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<CitaDto>(
            citas.Select(ToDto).ToArray(),
            query.PageNumber,
            query.PageSize,
            totalItems);
    }

    public async Task<ServiceResult<CitaDto>> GetMiCitaByIdAsync(
        CurrentUserContext currentUser,
        int idCita,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return ServiceResult<CitaDto>.Forbidden("Debes iniciar sesión para ver tus citas.");
        }

        var cita = await ApplyMisCitasScope(BaseQuery().AsNoTracking(), currentUser)
            .FirstOrDefaultAsync(item => item.IdCita == idCita, cancellationToken);

        return cita is null
            ? ServiceResult<CitaDto>.NotFound("La cita no existe o no pertenece al usuario autenticado.")
            : ServiceResult<CitaDto>.Success(ToDto(cita));
    }

    public async Task<ServiceResult<CitaHistorialTimelineDto>> GetMiCitaHistorialAsync(
        CurrentUserContext currentUser,
        int idCita,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return ServiceResult<CitaHistorialTimelineDto>.Forbidden("Debes iniciar sesión para ver el historial.");
        }

        var cita = await ApplyMisCitasScope(BaseQuery().AsNoTracking(), currentUser)
            .FirstOrDefaultAsync(item => item.IdCita == idCita, cancellationToken);

        return cita is null
            ? ServiceResult<CitaHistorialTimelineDto>.NotFound("La cita no existe o no pertenece al usuario autenticado.")
            : ServiceResult<CitaHistorialTimelineDto>.Success(ToTimelineDto(cita, exposeInternalDetails: false, currentUser.UserId));
    }

    public async Task<ServiceResult<CitaDto>> CancelarMiCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        var cita = await GetEditableClientCitaAsync(currentUser, idCita, cancellationToken);
        if (cita is null)
        {
            return ServiceResult<CitaDto>.NotFound("La cita no existe o no pertenece al cliente autenticado.");
        }

        var validationErrors = await ValidateClientCanModifyAsync(cita, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<CitaDto>.Validation(validationErrors);
        }

        var estado = await dbContext.EstadosCita
            .FirstOrDefaultAsync(item => item.Nombre == CancelledStateName && item.Activo, cancellationToken);

        if (estado is null)
        {
            return ServiceResult<CitaDto>.Validation([
                new ValidationError(string.Empty, $"El estado de cita '{CancelledStateName}' no existe o no está activo.")
            ]);
        }

        var pagoSnapshot = citaPagoImpactService.Capture(cita);
        var estadoAnterior = cita.IdEstadoCita;
        cita.IdEstadoCita = estado.IdEstadoCita;
        cita.FechaActualizacion = DateTime.Now;
        AddHistorial(cita, estadoAnterior, estado.IdEstadoCita, currentUser.UserId, request.Observacion ?? "Cita cancelada por el cliente.");
        await citaPagoImpactService.RegistrarCancelacionAsync(
            cita,
            pagoSnapshot,
            currentUser.UserId,
            request.Observacion ?? "Cita cancelada por el cliente.",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await notificacionService.CrearPorCambioEstadoAsync(cita.IdCita, estado.Nombre, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(cita.IdNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdCita == cita.IdCita, cancellationToken);

        return ServiceResult<CitaDto>.Success(ToDto(updated));
    }

    public async Task<ServiceResult<CitaDto>> ReagendarMiCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        ReagendarCitaRequest request,
        CancellationToken cancellationToken)
    {
        var idNegocio = await dbContext.Citas
            .AsNoTracking()
            .Where(cita => cita.IdCita == idCita && cita.Cliente.UserId == currentUser.UserId)
            .Select(cita => (int?)cita.IdNegocio)
            .FirstOrDefaultAsync(cancellationToken);

        if (!idNegocio.HasValue)
        {
            return ServiceResult<CitaDto>.NotFound("La cita no existe o no pertenece al cliente autenticado.");
        }

        return await CitaConcurrencyGuard.ExecuteWithBusinessScheduleLockAsync(
            dbContext,
            idNegocio.Value,
            async () =>
            {
                var cita = await GetEditableClientCitaAsync(currentUser, idCita, cancellationToken);
                if (cita is null)
                {
                    return ServiceResult<CitaDto>.NotFound("La cita no existe o no pertenece al cliente autenticado.");
                }

                var clientValidationErrors = await ValidateClientCanModifyAsync(cita, cancellationToken);
                if (clientValidationErrors.Count > 0)
                {
                    return ServiceResult<CitaDto>.Validation(clientValidationErrors);
                }

                var idPrestador = request.IdPrestador ?? cita.IdPrestador;
                var validationContext = await BuildValidationContextAsync(
                    cita.IdNegocio,
                    cita.IdServicio,
                    idPrestador,
                    cita.IdEstadoCita,
                    request.FechaInicio,
                    request.FechaFin,
                    cita.PrecioEstimado,
                    cita.CamposValor.Select(campo => new CitaCampoValorRequest(campo.IdCampoReserva, campo.Valor)).ToArray(),
                    idCita,
                    cancellationToken);

                var validationErrors = await ValidateCitaRequestAsync(cita.IdNegocio, cita.IdCliente, validationContext, cancellationToken);
                if (validationErrors.Count > 0)
                {
                    return ServiceResult<CitaDto>.Validation(validationErrors);
                }

                var pagoSnapshot = citaPagoImpactService.Capture(cita);
                var estadoAnterior = cita.IdEstadoCita;
                var estadoReagendada = await dbContext.EstadosCita
                    .FirstOrDefaultAsync(estado => estado.Nombre == RescheduledStateName && estado.Activo, cancellationToken);

                cita.IdPrestador = idPrestador;
                cita.FechaInicio = request.FechaInicio;
                cita.FechaFin = validationContext.FechaFin;
                cita.FechaActualizacion = DateTime.Now;
                if (estadoReagendada is not null)
                {
                    cita.IdEstadoCita = estadoReagendada.IdEstadoCita;
                }

                AddHistorial(cita, estadoAnterior, cita.IdEstadoCita, currentUser.UserId, request.Observacion ?? "Cita reagendada por el cliente.");
                await citaPagoImpactService.RegistrarReagendamientoAsync(
                    cita,
                    pagoSnapshot,
                    currentUser.UserId,
                    request.Observacion ?? "Cita reagendada por el cliente.",
                    cancellationToken);

                await dbContext.SaveChangesAsync(cancellationToken);
                await notificacionService.CrearPorCambioEstadoAsync(cita.IdCita, RescheduledStateName, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                var updated = await BaseQuery(cita.IdNegocio)
                    .AsNoTracking()
                    .FirstAsync(item => item.IdCita == cita.IdCita, cancellationToken);

                return ServiceResult<CitaDto>.Success(ToDto(updated));
            },
            cancellationToken);
    }

    public async Task<ServiceResult<CitaDto>> GetMiAgendaCitaByIdAsync(
        CurrentUserContext currentUser,
        int idCita,
        CancellationToken cancellationToken)
    {
        var context = await GetAssignedAgendaCitaContextAsync(currentUser, idCita, cancellationToken);
        if (context is null)
        {
            return ServiceResult<CitaDto>.NotFound("La cita no existe o no pertenece a tu agenda.");
        }

        var cita = await BaseQuery(context.IdNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdCita == idCita, cancellationToken);

        return ServiceResult<CitaDto>.Success(ToDto(cita));
    }

    public async Task<ServiceResult<CitaDto>> ReagendarMiAgendaCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        ReagendarCitaRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetAssignedAgendaCitaContextAsync(currentUser, idCita, cancellationToken);
        if (context is null)
        {
            return ServiceResult<CitaDto>.NotFound("La cita no existe o no pertenece a tu agenda.");
        }

        if (request.IdPrestador.HasValue &&
            !await IsAssignedProfessionalAsync(currentUser, context.IdNegocio, request.IdPrestador.Value, cancellationToken))
        {
            return ServiceResult<CitaDto>.Forbidden("No puedes mover la cita a un prestador que no pertenece a tu agenda.");
        }

        return await ReagendarAsync(currentUser, context.IdNegocio, idCita, request, cancellationToken);
    }

    public async Task<ServiceResult<CitaDto>> ConfirmarMiAgendaCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetAssignedAgendaCitaContextAsync(currentUser, idCita, cancellationToken);
        return context is null
            ? ServiceResult<CitaDto>.NotFound("La cita no existe o no pertenece a tu agenda.")
            : await ConfirmarAsync(currentUser, context.IdNegocio, idCita, request, cancellationToken);
    }

    public async Task<ServiceResult<CitaDto>> CancelarMiAgendaCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetAssignedAgendaCitaContextAsync(currentUser, idCita, cancellationToken);
        return context is null
            ? ServiceResult<CitaDto>.NotFound("La cita no existe o no pertenece a tu agenda.")
            : await CancelarAsync(currentUser, context.IdNegocio, idCita, request, cancellationToken);
    }

    public async Task<ServiceResult<CitaDto>> MarcarAtendidaMiAgendaCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetAssignedAgendaCitaContextAsync(currentUser, idCita, cancellationToken);
        return context is null
            ? ServiceResult<CitaDto>.NotFound("La cita no existe o no pertenece a tu agenda.")
            : await MarcarAtendidaAsync(currentUser, context.IdNegocio, idCita, request, cancellationToken);
    }

    public async Task<ServiceResult<CitaDto>> MarcarNoAsistioMiAgendaCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CitaActionRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetAssignedAgendaCitaContextAsync(currentUser, idCita, cancellationToken);
        return context is null
            ? ServiceResult<CitaDto>.NotFound("La cita no existe o no pertenece a tu agenda.")
            : await MarcarNoAsistioAsync(currentUser, context.IdNegocio, idCita, request, cancellationToken);
    }

    public async Task<ServiceResult<CitaDto>> ActualizarNotaInternaMiAgendaCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        UpdateNotaInternaCitaRequest request,
        CancellationToken cancellationToken)
    {
        var context = await GetAssignedAgendaCitaContextAsync(currentUser, idCita, cancellationToken);
        if (context is null)
        {
            return ServiceResult<CitaDto>.NotFound("La cita no existe o no pertenece a tu agenda.");
        }

        var cita = await EditableQuery(context.IdNegocio)
            .FirstOrDefaultAsync(item => item.IdCita == idCita, cancellationToken);
        if (cita is null)
        {
            return ServiceResult<CitaDto>.NotFound("La cita no existe.");
        }

        cita.NotaInterna = request.NotaInterna?.Trim();
        cita.FechaActualizacion = DateTime.Now;
        AddHistorial(
            cita,
            cita.IdEstadoCita,
            cita.IdEstadoCita,
            currentUser.UserId,
            request.Observacion ?? "Nota interna actualizada desde mi agenda.");

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery(context.IdNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdCita == idCita, cancellationToken);

        return ServiceResult<CitaDto>.Success(ToDto(updated));
    }

    private async Task<ServiceResult<CitaDto>> CambiarEstadoPorNombreAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        string estadoNombre,
        string? observacion,
        CancellationToken cancellationToken)
    {
        var estado = await dbContext.EstadosCita
            .FirstOrDefaultAsync(item => item.Nombre == estadoNombre && item.Activo, cancellationToken);

        if (estado is null)
        {
            return ServiceResult<CitaDto>.Validation([
                new ValidationError(string.Empty, $"El estado de cita '{estadoNombre}' no existe o no está activo.")
            ]);
        }

        return await CambiarEstadoInternalAsync(currentUser, idNegocio, idCita, estado, observacion, cancellationToken);
    }

    private async Task<ServiceResult<CitaDto>> CambiarEstadoInternalAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        EstadoCita estadoNuevo,
        string? observacion,
        CancellationToken cancellationToken)
    {
        var accessResult = await ValidateAccessAsync(currentUser, idNegocio, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var cita = await EditableQuery(idNegocio).FirstOrDefaultAsync(item => item.IdCita == idCita, cancellationToken);
        if (cita is null)
        {
            return ServiceResult<CitaDto>.NotFound("La cita no existe.");
        }

        if (!await CanManageAgendaAsync(currentUser, idNegocio, cancellationToken) &&
            !await IsAssignedProfessionalAsync(currentUser, idNegocio, cita.IdPrestador, cancellationToken))
        {
            return ServiceResult<CitaDto>.Forbidden("No tienes acceso para cambiar el estado de esta cita.");
        }

        var pagoSnapshot = citaPagoImpactService.Capture(cita);
        var estadoAnterior = cita.IdEstadoCita;
        cita.IdEstadoCita = estadoNuevo.IdEstadoCita;
        cita.FechaActualizacion = DateTime.Now;
        AddHistorial(cita, estadoAnterior, estadoNuevo.IdEstadoCita, currentUser.UserId, observacion);
        if (estadoNuevo.Nombre.Equals(CancelledStateName, StringComparison.OrdinalIgnoreCase))
        {
            await citaPagoImpactService.RegistrarCancelacionAsync(
                cita,
                pagoSnapshot,
                currentUser.UserId,
                observacion ?? "Cita cancelada.",
                cancellationToken);
        }
        else if (estadoNuevo.Nombre.Equals(RescheduledStateName, StringComparison.OrdinalIgnoreCase))
        {
            await citaPagoImpactService.RegistrarReagendamientoAsync(
                cita,
                pagoSnapshot,
                currentUser.UserId,
                observacion ?? "Cita reagendada.",
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (estadoAnterior != estadoNuevo.IdEstadoCita)
        {
            await notificacionService.CrearPorCambioEstadoAsync(idCita, estadoNuevo.Nombre, cancellationToken);
            if (estadoNuevo.Nombre.Equals(AttendedStateName, StringComparison.OrdinalIgnoreCase))
            {
                await resenaNegocioService.CrearSolicitudPostAtencionAsync(idCita, cancellationToken);
            }
            else
            {
                await resenaNegocioService.CancelarSolicitudesPendientesCitaAsync(idCita, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var updated = await BaseQuery(idNegocio)
            .AsNoTracking()
            .FirstAsync(item => item.IdCita == idCita, cancellationToken);

        return ServiceResult<CitaDto>.Success(ToDto(updated));
    }

    private IQueryable<Cita> BaseQuery(int idNegocio)
    {
        return BaseQuery()
            .Where(cita => cita.IdNegocio == idNegocio);
    }

    private IQueryable<Cita> BaseQuery()
    {
        return dbContext.Citas
            .Include(cita => cita.Negocio)
            .Include(cita => cita.Cliente)
            .Include(cita => cita.Servicio)
            .Include(cita => cita.Prestador)
            .Include(cita => cita.EstadoCita)
            .Include(cita => cita.CamposValor)
                .ThenInclude(valor => valor.CampoReserva)
            .Include(cita => cita.Historial)
                .ThenInclude(historial => historial.EstadoAnterior)
            .Include(cita => cita.Historial)
                .ThenInclude(historial => historial.EstadoNuevo)
            .Include(cita => cita.Historial)
                .ThenInclude(historial => historial.Usuario)
            ;
    }

    private IQueryable<Cita> EditableQuery(int idNegocio)
    {
        return dbContext.Citas
            .Include(cita => cita.Cliente)
            .Include(cita => cita.EstadoCita)
            .Include(cita => cita.CamposValor)
            .Where(cita => cita.IdNegocio == idNegocio);
    }

    private async Task<Cita?> GetEditableClientCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return null;
        }

        return await dbContext.Citas
            .Include(cita => cita.Cliente)
            .Include(cita => cita.EstadoCita)
            .Include(cita => cita.CamposValor)
            .FirstOrDefaultAsync(
                cita => cita.IdCita == idCita && cita.Cliente.UserId == currentUser.UserId,
                cancellationToken);
    }

    private async Task<AssignedAgendaCitaContext?> GetAssignedAgendaCitaContextAsync(
        CurrentUserContext currentUser,
        int idCita,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return null;
        }

        return await dbContext.Citas
            .AsNoTracking()
            .Where(cita =>
                cita.IdCita == idCita &&
                cita.IdPrestador.HasValue &&
                cita.Prestador != null &&
                cita.Prestador.UserId == currentUser.UserId &&
                cita.Prestador.Activo &&
                cita.Negocio.Activo)
            .Select(cita => new AssignedAgendaCitaContext(
                cita.IdNegocio,
                cita.IdCita,
                cita.IdPrestador!.Value))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<List<ValidationError>> ValidateClientCanModifyAsync(
        Cita cita,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (cita.EstadoCita.EsEstadoFinal)
        {
            errors.Add(new ValidationError(string.Empty, "La cita ya está en un estado final."));
            return errors;
        }

        var regla = await dbContext.ReglasReserva
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdNegocio == cita.IdNegocio, cancellationToken);

        if (regla?.PermiteCancelacionCliente == false)
        {
            errors.Add(new ValidationError(string.Empty, "El negocio no permite que el cliente cancele o reagende citas."));
        }

        var horasLimite = regla?.HorasLimiteCancelacion ?? 6;
        if (DateTime.Now.AddHours(horasLimite) > cita.FechaInicio)
        {
            errors.Add(new ValidationError(string.Empty, $"La cita solo puede modificarse hasta {horasLimite} horas antes."));
        }

        return errors;
    }

    private static IQueryable<Cita> ApplyCitaFilters(IQueryable<Cita> citasQuery, CitaQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            citasQuery = citasQuery.Where(cita =>
                cita.Codigo.Contains(search) ||
                cita.Cliente.Nombre.Contains(search) ||
                cita.Servicio.Nombre.Contains(search) ||
                (cita.Prestador != null && cita.Prestador.Nombre.Contains(search)));
        }

        if (query.IdCliente.HasValue)
        {
            citasQuery = citasQuery.Where(cita => cita.IdCliente == query.IdCliente.Value);
        }

        if (query.IdServicio.HasValue)
        {
            citasQuery = citasQuery.Where(cita => cita.IdServicio == query.IdServicio.Value);
        }

        if (query.IdPrestador.HasValue)
        {
            citasQuery = citasQuery.Where(cita => cita.IdPrestador == query.IdPrestador.Value);
        }

        if (query.IdEstadoCita.HasValue)
        {
            citasQuery = citasQuery.Where(cita => cita.IdEstadoCita == query.IdEstadoCita.Value);
        }

        if (query.FechaDesde.HasValue)
        {
            citasQuery = citasQuery.Where(cita => cita.FechaFin >= query.FechaDesde.Value);
        }

        if (query.FechaHasta.HasValue)
        {
            citasQuery = citasQuery.Where(cita => cita.FechaInicio <= query.FechaHasta.Value);
        }

        if (query.SoloEstadosActivos == true)
        {
            citasQuery = citasQuery.Where(cita => !cita.EstadoCita.EsEstadoFinal);
        }

        return citasQuery;
    }

    private IQueryable<Cita> ApplyUserScope(IQueryable<Cita> query, CurrentUserContext currentUser)
    {
        if (currentUser.IsSuperAdmin)
        {
            return query;
        }

        return query.Where(cita =>
            cita.Negocio.NegocioUsuarios.Any(usuario =>
                usuario.UserId == currentUser.UserId &&
                usuario.Activo &&
                (usuario.RolNegocio.Nombre == OwnerRoleName ||
                    usuario.RolNegocio.Nombre == AdminRoleName ||
                    usuario.RolNegocio.Nombre == RecepcionistaRoleName)) ||
            (cita.Prestador != null && cita.Prestador.UserId == currentUser.UserId));
    }

    private IQueryable<Cita> ApplyMisCitasScope(IQueryable<Cita> query, CurrentUserContext currentUser)
    {
        return query.Where(cita =>
            cita.Cliente.UserId == currentUser.UserId);
    }

    private async Task<ServiceResult<CitaDto>?> ValidateAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<CitaDto>.NotFound("El negocio no existe.");
        }

        if (!await CanAccessAgendaAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<CitaDto>.Forbidden("No tienes acceso a la agenda de este negocio.");
        }

        return null;
    }

    private async Task<ServiceResult<CitaDto>?> ValidateManagerAccessAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (!await NegocioExistsAsync(idNegocio, cancellationToken))
        {
            return ServiceResult<CitaDto>.NotFound("El negocio no existe.");
        }

        if (!await CanManageAgendaAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<CitaDto>.Forbidden("No tienes acceso para administrar citas de este negocio.");
        }

        return null;
    }

    private async Task<bool> NegocioExistsAsync(int idNegocio, CancellationToken cancellationToken)
    {
        return await dbContext.Negocios.AnyAsync(negocio => negocio.IdNegocio == idNegocio, cancellationToken);
    }

    private async Task<bool> CanAccessAgendaAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        return await CanManageAgendaAsync(currentUser, idNegocio, cancellationToken) ||
            await dbContext.Prestadores.AnyAsync(
                prestador =>
                    prestador.IdNegocio == idNegocio &&
                    prestador.UserId == currentUser.UserId &&
                    prestador.Activo,
                cancellationToken);
    }

    private async Task<bool> CanManageAgendaAsync(
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
                    item.RolNegocio.Nombre == RecepcionistaRoleName),
            cancellationToken);
    }

    private async Task<bool> IsAssignedProfessionalAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int? idPrestador,
        CancellationToken cancellationToken)
    {
        if (!idPrestador.HasValue)
        {
            return false;
        }

        return await dbContext.Prestadores.AnyAsync(
            prestador =>
                prestador.IdNegocio == idNegocio &&
                prestador.IdPrestador == idPrestador.Value &&
                prestador.UserId == currentUser.UserId &&
                prestador.Activo,
            cancellationToken);
    }

    private async Task<CitaValidationContext> BuildValidationContextAsync(
        int idNegocio,
        int idServicio,
        int? idPrestador,
        int? idEstadoCita,
        DateTime fechaInicio,
        DateTime? fechaFin,
        decimal? precioEstimado,
        IReadOnlyCollection<CitaCampoValorRequest>? camposValor,
        int? currentIdCita,
        CancellationToken cancellationToken)
    {
        var servicio = await dbContext.Servicios
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdNegocio == idNegocio && item.IdServicio == idServicio && item.Activo, cancellationToken);

        var regla = await dbContext.ReglasReserva
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdNegocio == idNegocio, cancellationToken);

        var calculatedFechaFin = fechaFin ?? fechaInicio.AddMinutes(servicio?.DuracionMinutos ?? 0);
        var estado = idEstadoCita.HasValue
            ? await dbContext.EstadosCita.FirstOrDefaultAsync(item => item.IdEstadoCita == idEstadoCita.Value && item.Activo, cancellationToken)
            : await GetDefaultEstadoAsync(
                servicio?.RequierePagoAnticipado == true,
                regla?.RequiereConfirmacionManual ?? false,
                cancellationToken);

        return new CitaValidationContext(
            servicio,
            regla,
            estado,
            calculatedFechaFin,
            precioEstimado ?? servicio?.Precio ?? 0m,
            camposValor ?? [],
            currentIdCita)
        {
            FechaInicio = fechaInicio,
            IdPrestador = idPrestador
        };
    }

    private async Task<List<ValidationError>> ValidateCitaRequestAsync(
        int idNegocio,
        int idCliente,
        CitaValidationContext context,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (!await dbContext.Clientes.AnyAsync(cliente => cliente.IdNegocio == idNegocio && cliente.IdCliente == idCliente && cliente.Activo, cancellationToken))
        {
            errors.Add(new ValidationError(nameof(CreateCitaRequest.IdCliente), "El cliente indicado no existe o no está activo para este negocio."));
        }

        if (context.Servicio is null)
        {
            errors.Add(new ValidationError(nameof(CreateCitaRequest.IdServicio), "El servicio indicado no existe o no está activo para este negocio."));
        }

        if (context.EstadoCita is null)
        {
            errors.Add(new ValidationError(nameof(CreateCitaRequest.IdEstadoCita), "El estado de cita indicado no existe o no está activo."));
        }

        if (context.FechaFin <= context.FechaInicio)
        {
            errors.Add(new ValidationError(nameof(CreateCitaRequest.FechaFin), "La fecha de fin debe ser mayor que la fecha de inicio."));
        }

        if (context.PrecioEstimado < 0)
        {
            errors.Add(new ValidationError(nameof(CreateCitaRequest.PrecioEstimado), "El precio estimado no puede ser negativo."));
        }

        if (context.Servicio is not null)
        {
            await ValidatePrestadorAsync(idNegocio, context, errors, cancellationToken);
            await ValidateCamposPersonalizadosAsync(idNegocio, context.Servicio.IdServicio, context.CamposValor, errors, cancellationToken);
            await ValidateDisponibilidadAsync(idNegocio, context, errors, cancellationToken);
        }

        return errors;
    }

    private async Task ValidatePrestadorAsync(
        int idNegocio,
        CitaValidationContext context,
        List<ValidationError> errors,
        CancellationToken cancellationToken)
    {
        if (!context.IdPrestador.HasValue)
        {
            if (context.Servicio?.RequiereProfesional == true)
            {
                errors.Add(new ValidationError(nameof(CreateCitaRequest.IdPrestador), "El servicio requiere prestador o recurso."));
            }

            return;
        }

        var prestadorExists = await dbContext.Prestadores.AnyAsync(
            prestador =>
                prestador.IdNegocio == idNegocio &&
                prestador.IdPrestador == context.IdPrestador.Value &&
                prestador.Activo,
            cancellationToken);

        if (!prestadorExists)
        {
            errors.Add(new ValidationError(nameof(CreateCitaRequest.IdPrestador), "El prestador o recurso indicado no existe o no está activo para este negocio."));
            return;
        }

        var canServe = await dbContext.PrestadorServicios.AnyAsync(
            relacion =>
                relacion.IdNegocio == idNegocio &&
                relacion.IdPrestador == context.IdPrestador.Value &&
                relacion.IdServicio == context.Servicio!.IdServicio &&
                relacion.Activo,
            cancellationToken);

        if (!canServe)
        {
            errors.Add(new ValidationError(nameof(CreateCitaRequest.IdPrestador), "El prestador o recurso no tiene asignado el servicio indicado."));
        }
    }

    private async Task ValidateCamposPersonalizadosAsync(
        int idNegocio,
        int idServicio,
        IReadOnlyCollection<CitaCampoValorRequest> camposValor,
        List<ValidationError> errors,
        CancellationToken cancellationToken)
    {
        var duplicated = camposValor
            .GroupBy(campo => campo.IdCampoReserva)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicated is not null)
        {
            errors.Add(new ValidationError(nameof(CreateCitaRequest.CamposValor), "No puedes enviar el mismo campo personalizado más de una vez."));
            return;
        }

        var ids = camposValor.Select(campo => campo.IdCampoReserva).ToArray();
        var campos = await dbContext.CamposReserva
            .AsNoTracking()
            .Include(campo => campo.TipoCampo)
            .Where(campo =>
                campo.IdNegocio == idNegocio &&
                campo.Activo &&
                (!campo.IdServicio.HasValue || campo.IdServicio == idServicio))
            .ToArrayAsync(cancellationToken);

        var missing = ids.Where(id => campos.All(campo => campo.IdCampoReserva != id)).ToArray();
        if (missing.Length > 0)
        {
            errors.Add(new ValidationError(nameof(CreateCitaRequest.CamposValor), "Uno o más campos personalizados no existen o no están activos para este negocio."));
        }

        foreach (var obligatorio in campos.Where(campo => campo.Obligatorio))
        {
            var value = camposValor.FirstOrDefault(campo => campo.IdCampoReserva == obligatorio.IdCampoReserva)?.Valor;
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new ValidationError(nameof(CreateCitaRequest.CamposValor), $"El campo '{obligatorio.Etiqueta}' es obligatorio."));
            }
        }

        var selectFields = campos
            .Where(campo => campo.TipoCampo.Nombre == "Select")
            .ToArray();

        foreach (var campoValor in camposValor.Where(campo => !string.IsNullOrWhiteSpace(campo.Valor)))
        {
            var campo = selectFields.FirstOrDefault(item => item.IdCampoReserva == campoValor.IdCampoReserva);
            if (campo is null)
            {
                continue;
            }

            var optionExists = await dbContext.CampoReservaOpciones.AnyAsync(
                opcion =>
                    opcion.IdNegocio == idNegocio &&
                    opcion.IdCampoReserva == campo.IdCampoReserva &&
                    opcion.Valor == campoValor.Valor &&
                    opcion.Activo,
                cancellationToken);

            if (!optionExists)
            {
                errors.Add(new ValidationError(nameof(CreateCitaRequest.CamposValor), $"El valor del campo '{campo.Etiqueta}' no es una opción válida."));
            }
        }
    }

    private async Task ValidateDisponibilidadAsync(
        int idNegocio,
        CitaValidationContext context,
        List<ValidationError> errors,
        CancellationToken cancellationToken)
    {
        var fechaFinConPreparacion = context.FechaFin.AddMinutes(context.Servicio?.TiempoPreparacionMinutos ?? 0);

        if (context.ReglaReserva is not null)
        {
            var minFechaInicio = DateTime.Now.AddHours(context.ReglaReserva.MinHorasAnticipacion);
            var maxFechaPermitida = DateTime.Now.Date.AddDays(context.ReglaReserva.MaxDiasAdelanto);

            if (context.FechaInicio < minFechaInicio)
            {
                errors.Add(new ValidationError(nameof(CreateCitaRequest.FechaInicio), "La fecha no cumple con la anticipación mínima configurada."));
            }

            if (context.FechaInicio.Date > maxFechaPermitida)
            {
            errors.Add(new ValidationError(nameof(CreateCitaRequest.FechaInicio), "La fecha supera el máximo de días de adelanto configurado."));
            }
        }

        await ValidateHorarioDisponibleAsync(idNegocio, context, fechaFinConPreparacion, errors, cancellationToken);

        var hasBloqueo = await dbContext.BloqueosHorario.AnyAsync(
            bloqueo =>
                bloqueo.IdNegocio == idNegocio &&
                bloqueo.Activo &&
                (!bloqueo.IdPrestador.HasValue || bloqueo.IdPrestador == context.IdPrestador) &&
                bloqueo.FechaInicio < fechaFinConPreparacion &&
                bloqueo.FechaFin > context.FechaInicio,
            cancellationToken);

        if (hasBloqueo)
        {
            errors.Add(new ValidationError(nameof(CreateCitaRequest.FechaInicio), "Existe un bloqueo de horario para el rango seleccionado."));
        }

        if (context.ReglaReserva?.PermiteSobreturnos == true || !context.IdPrestador.HasValue)
        {
            return;
        }

        var overlaps = await dbContext.Citas.AnyAsync(
            cita =>
                cita.IdNegocio == idNegocio &&
                cita.IdPrestador == context.IdPrestador.Value &&
                !cita.EstadoCita.EsEstadoFinal &&
                cita.FechaInicio < fechaFinConPreparacion &&
                cita.FechaFin.AddMinutes(cita.Servicio.TiempoPreparacionMinutos) > context.FechaInicio &&
                (!context.CurrentIdCita.HasValue || cita.IdCita != context.CurrentIdCita.Value),
            cancellationToken);

        if (overlaps)
        {
            errors.Add(new ValidationError(nameof(CreateCitaRequest.FechaInicio), "El prestador o recurso ya tiene una cita activa en ese horario."));
        }
    }

    private async Task ValidateHorarioDisponibleAsync(
        int idNegocio,
        CitaValidationContext context,
        DateTime fechaFinConPreparacion,
        List<ValidationError> errors,
        CancellationToken cancellationToken)
    {
        if (fechaFinConPreparacion.Date != context.FechaInicio.Date)
        {
            errors.Add(new ValidationError(nameof(CreateCitaRequest.FechaFin), "La cita debe quedar dentro de un mismo día de atención."));
            return;
        }

        var fecha = DateOnly.FromDateTime(context.FechaInicio);
        var diaSemana = GetDiaSemana(fecha);
        var horariosNegocio = await dbContext.HorariosNegocio
            .AsNoTracking()
            .Where(horario => horario.IdNegocio == idNegocio && horario.DiaSemana == diaSemana && horario.Activo)
            .Select(horario => new CitaTimeRange(fecha.ToDateTime(horario.HoraInicio), fecha.ToDateTime(horario.HoraFin)))
            .ToArrayAsync(cancellationToken);
        var rangosBase = MergeRanges(horariosNegocio);

        if (rangosBase.Count == 0)
        {
            errors.Add(new ValidationError(nameof(CreateCitaRequest.FechaInicio), "El negocio no tiene horario disponible para la fecha seleccionada."));
            return;
        }

        if (context.IdPrestador.HasValue)
        {
            var horariosPrestador = await dbContext.HorariosPrestador
                .AsNoTracking()
                .Where(horario =>
                    horario.IdNegocio == idNegocio &&
                    horario.IdPrestador == context.IdPrestador.Value &&
                    horario.DiaSemana == diaSemana &&
                    horario.Activo)
                .Select(horario => new CitaTimeRange(fecha.ToDateTime(horario.HoraInicio), fecha.ToDateTime(horario.HoraFin)))
                .ToArrayAsync(cancellationToken);
            var rangosPrestador = MergeRanges(horariosPrestador);

            if (rangosPrestador.Count == 0)
            {
                errors.Add(new ValidationError(nameof(CreateCitaRequest.IdPrestador), "El prestador o recurso no tiene horario disponible para la fecha seleccionada."));
                return;
            }

            rangosBase = Intersect(rangosBase, rangosPrestador);
        }

        var dentroDeHorario = rangosBase.Any(rango =>
            rango.Inicio <= context.FechaInicio &&
            rango.Fin >= fechaFinConPreparacion);

        if (!dentroDeHorario)
        {
            errors.Add(new ValidationError(nameof(CreateCitaRequest.FechaInicio), "El horario seleccionado queda fuera de la disponibilidad configurada."));
        }
    }

    private async Task<EstadoCita?> GetDefaultEstadoAsync(
        bool requierePagoAnticipado,
        bool requiereConfirmacionManual,
        CancellationToken cancellationToken)
    {
        var name = requierePagoAnticipado
            ? PaymentPendingStateName
            : requiereConfirmacionManual
                ? PendingStateName
                : ConfirmedStateName;
        return await dbContext.EstadosCita.FirstOrDefaultAsync(estado => estado.Nombre == name && estado.Activo, cancellationToken)
            ?? await dbContext.EstadosCita.FirstOrDefaultAsync(estado => estado.Activo, cancellationToken);
    }

    private async Task<string> GenerateCodigoAsync(int idNegocio, CancellationToken cancellationToken)
    {
        var negocio = await dbContext.Negocios
            .AsNoTracking()
            .FirstAsync(item => item.IdNegocio == idNegocio, cancellationToken);
        var prefix = new string(negocio.Slug.Where(char.IsLetterOrDigit).Take(4).ToArray()).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = $"NEG{idNegocio}";
        }

        var year = DateTime.Now.Year;
        var count = await dbContext.Citas.CountAsync(
            cita => cita.IdNegocio == idNegocio && cita.FechaCreacion.Year == year,
            cancellationToken);

        return $"{prefix}-{year}-{count + 1:000000}";
    }

    private static void AddCampoValores(Cita cita, IReadOnlyCollection<CitaCampoValorRequest>? camposValor)
    {
        foreach (var campo in camposValor ?? [])
        {
            cita.CamposValor.Add(new CitaCampoValor
            {
                IdNegocio = cita.IdNegocio,
                IdCampoReserva = campo.IdCampoReserva,
                Valor = campo.Valor?.Trim()
            });
        }
    }

    private void ReplaceCampoValores(Cita cita, IReadOnlyCollection<CitaCampoValorRequest>? camposValor)
    {
        var requestedValues = camposValor ?? [];
        var requestedIds = requestedValues
            .Select(campo => campo.IdCampoReserva)
            .ToHashSet();

        var removedValues = cita.CamposValor
            .Where(valor => !requestedIds.Contains(valor.IdCampoReserva))
            .ToArray();

        dbContext.CitaCampoValores.RemoveRange(removedValues);

        foreach (var campo in requestedValues)
        {
            var existing = cita.CamposValor.FirstOrDefault(valor => valor.IdCampoReserva == campo.IdCampoReserva);
            if (existing is null)
            {
                cita.CamposValor.Add(new CitaCampoValor
                {
                    IdNegocio = cita.IdNegocio,
                    IdCampoReserva = campo.IdCampoReserva,
                    Valor = campo.Valor?.Trim()
                });

                continue;
            }

            existing.Valor = campo.Valor?.Trim();
        }
    }

    private static void AddHistorial(
        Cita cita,
        int? idEstadoAnterior,
        int idEstadoNuevo,
        string userId,
        string? observacion)
    {
        cita.Historial.Add(new CitaHistorial
        {
            IdNegocio = cita.IdNegocio,
            IdEstadoAnterior = idEstadoAnterior,
            IdEstadoNuevo = idEstadoNuevo,
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
            Observacion = observacion?.Trim()
        });
    }

    private static byte GetDiaSemana(DateOnly fecha)
    {
        return fecha.DayOfWeek switch
        {
            DayOfWeek.Monday => 1,
            DayOfWeek.Tuesday => 2,
            DayOfWeek.Wednesday => 3,
            DayOfWeek.Thursday => 4,
            DayOfWeek.Friday => 5,
            DayOfWeek.Saturday => 6,
            DayOfWeek.Sunday => 7,
            _ => 0
        };
    }

    private static IReadOnlyCollection<CitaTimeRange> Intersect(
        IReadOnlyCollection<CitaTimeRange> first,
        IReadOnlyCollection<CitaTimeRange> second)
    {
        var intersections = new List<CitaTimeRange>();

        foreach (var left in first)
        {
            foreach (var right in second)
            {
                var inicio = left.Inicio > right.Inicio ? left.Inicio : right.Inicio;
                var fin = left.Fin < right.Fin ? left.Fin : right.Fin;

                if (fin > inicio)
                {
                    intersections.Add(new CitaTimeRange(inicio, fin));
                }
            }
        }

        return MergeRanges(intersections);
    }

    private static IReadOnlyCollection<CitaTimeRange> MergeRanges(IEnumerable<CitaTimeRange> ranges)
    {
        var ordered = ranges
            .Where(range => range.Fin > range.Inicio)
            .OrderBy(range => range.Inicio)
            .ThenBy(range => range.Fin)
            .ToArray();

        if (ordered.Length == 0)
        {
            return [];
        }

        var result = new List<CitaTimeRange>();
        var current = ordered[0];

        foreach (var range in ordered.Skip(1))
        {
            if (range.Inicio <= current.Fin)
            {
                current = current with
                {
                    Fin = range.Fin > current.Fin ? range.Fin : current.Fin
                };
                continue;
            }

            result.Add(current);
            current = range;
        }

        result.Add(current);
        return result;
    }

    private static CitaDto ToDto(Cita cita)
    {
        return new CitaDto(
            cita.IdCita,
            cita.IdNegocio,
            cita.Negocio.Nombre,
            cita.IdCliente,
            cita.Cliente.Nombre,
            cita.IdServicio,
            cita.Servicio.Nombre,
            cita.IdPrestador,
            cita.Prestador?.Nombre,
            cita.IdEstadoCita,
            cita.EstadoCita.Nombre,
            cita.EstadoCita.EsEstadoFinal,
            cita.Codigo,
            cita.FechaInicio,
            cita.FechaFin,
            cita.ComentarioCliente,
            cita.NotaInterna,
            cita.PrecioEstimado,
            cita.FechaCreacion,
            cita.FechaActualizacion,
            cita.CamposValor
                .OrderBy(valor => valor.CampoReserva.Orden)
                .Select(valor => new CitaCampoValorDto(
                    valor.IdCitaCampoValor,
                    valor.IdCampoReserva,
                    valor.CampoReserva.Etiqueta,
                    valor.CampoReserva.NombreInterno,
                    valor.Valor))
                .ToArray(),
            cita.Historial
                .OrderByDescending(historial => historial.FechaCreacion)
                .Select(historial => new CitaHistorialDto(
                    historial.IdCitaHistorial,
                    historial.IdEstadoAnterior,
                    historial.EstadoAnterior?.Nombre,
                    historial.IdEstadoNuevo,
                    historial.EstadoNuevo.Nombre,
                    historial.UserId,
                    historial.Usuario?.Email ?? historial.Usuario?.UserName,
                    historial.Observacion,
                    historial.FechaCreacion))
                .ToArray());
    }

    private static CitaHistorialTimelineDto ToTimelineDto(
        Cita cita,
        bool exposeInternalDetails,
        string currentUserId)
    {
        return new CitaHistorialTimelineDto(
            cita.IdCita,
            cita.IdNegocio,
            cita.Negocio.Nombre,
            cita.Codigo,
            cita.IdCliente,
            cita.Cliente.Nombre,
            cita.IdServicio,
            cita.Servicio.Nombre,
            cita.IdPrestador,
            cita.Prestador?.Nombre,
            cita.IdEstadoCita,
            cita.EstadoCita.Nombre,
            cita.FechaInicio,
            cita.FechaFin,
            cita.FechaCreacion,
            cita.FechaActualizacion,
            cita.Historial
                .OrderByDescending(historial => historial.FechaCreacion)
                .Select(historial => ToTimelineEvent(historial, exposeInternalDetails, currentUserId))
                .ToArray());
    }

    private static CitaHistorialEventoDto ToTimelineEvent(
        CitaHistorial historial,
        bool exposeInternalDetails,
        string currentUserId)
    {
        var tipoEvento = GetTimelineEventType(historial);
        var titulo = GetTimelineTitle(tipoEvento, historial.EstadoNuevo.Nombre);
        var descripcion = exposeInternalDetails
            ? historial.Observacion ?? BuildTimelineDescription(historial)
            : BuildTimelineDescription(historial);
        var usuario = exposeInternalDetails
            ? historial.Usuario?.Email ?? historial.Usuario?.UserName
            : null;
        var userId = exposeInternalDetails ? historial.UserId : null;
        var actor = GetTimelineActor(historial, exposeInternalDetails, currentUserId);

        return new CitaHistorialEventoDto(
            historial.IdCitaHistorial,
            tipoEvento,
            titulo,
            descripcion,
            historial.IdEstadoAnterior,
            historial.EstadoAnterior?.Nombre,
            historial.IdEstadoNuevo,
            historial.EstadoNuevo.Nombre,
            historial.IdEstadoAnterior != historial.IdEstadoNuevo,
            actor,
            userId,
            usuario,
            historial.FechaCreacion);
    }

    private static string GetTimelineEventType(CitaHistorial historial)
    {
        if (!historial.IdEstadoAnterior.HasValue)
        {
            return "Creacion";
        }

        var estadoNuevo = historial.EstadoNuevo.Nombre;
        if (string.Equals(estadoNuevo, ConfirmedStateName, StringComparison.OrdinalIgnoreCase))
        {
            return "Confirmacion";
        }

        if (string.Equals(estadoNuevo, RescheduledStateName, StringComparison.OrdinalIgnoreCase))
        {
            return "Reagendamiento";
        }

        if (string.Equals(estadoNuevo, CancelledStateName, StringComparison.OrdinalIgnoreCase))
        {
            return "Cancelacion";
        }

        if (string.Equals(estadoNuevo, AttendedStateName, StringComparison.OrdinalIgnoreCase))
        {
            return "Atencion";
        }

        if (string.Equals(estadoNuevo, NoShowStateName, StringComparison.OrdinalIgnoreCase))
        {
            return "Inasistencia";
        }

        if (string.Equals(estadoNuevo, PaymentPendingStateName, StringComparison.OrdinalIgnoreCase))
        {
            return "PagoPendiente";
        }

        return historial.IdEstadoAnterior == historial.IdEstadoNuevo
            ? "Actualizacion"
            : "CambioEstado";
    }

    private static string GetTimelineTitle(string tipoEvento, string estadoNuevo)
    {
        return tipoEvento switch
        {
            "Creacion" => "Cita creada",
            "Confirmacion" => "Cita confirmada",
            "Reagendamiento" => "Cita reagendada",
            "Cancelacion" => "Cita cancelada",
            "Atencion" => "Cita atendida",
            "Inasistencia" => "Cliente no asistió",
            "PagoPendiente" => "Cita pendiente de pago",
            "Actualizacion" => "Cita actualizada",
            _ => $"Estado cambiado a {estadoNuevo}"
        };
    }

    private static string BuildTimelineDescription(CitaHistorial historial)
    {
        if (!historial.IdEstadoAnterior.HasValue)
        {
            return "La cita fue registrada en el sistema.";
        }

        if (historial.IdEstadoAnterior == historial.IdEstadoNuevo)
        {
            return "Se actualizaron datos de la cita.";
        }

        return $"La cita cambio de {historial.EstadoAnterior?.Nombre ?? "Sin estado"} a {historial.EstadoNuevo.Nombre}.";
    }

    private static string GetTimelineActor(
        CitaHistorial historial,
        bool exposeInternalDetails,
        string currentUserId)
    {
        if (string.IsNullOrWhiteSpace(historial.UserId))
        {
            return "Sistema";
        }

        if (!exposeInternalDetails)
        {
            return string.Equals(historial.UserId, currentUserId, StringComparison.Ordinal)
                ? "Cliente"
                : "Negocio";
        }

        return historial.Usuario?.Email ?? historial.Usuario?.UserName ?? "Usuario";
    }

    private static ServiceResult<CitaHistorialTimelineDto> ConvertAccessResult(ServiceResult<CitaDto> result)
    {
        return result.Status switch
        {
            ServiceResultStatus.NotFound => ServiceResult<CitaHistorialTimelineDto>.NotFound(result.Errors.FirstOrDefault() ?? "No encontrado."),
            ServiceResultStatus.Forbidden => ServiceResult<CitaHistorialTimelineDto>.Forbidden(result.Errors.FirstOrDefault() ?? "No autorizado."),
            ServiceResultStatus.Validation => ServiceResult<CitaHistorialTimelineDto>.Validation(result.ValidationErrors),
            _ => ServiceResult<CitaHistorialTimelineDto>.Validation(result.Errors)
        };
    }

    private sealed record CitaTimeRange(DateTime Inicio, DateTime Fin);

    private sealed record CitaValidationContext(
        Servicio? Servicio,
        ReglaReserva? ReglaReserva,
        EstadoCita? EstadoCita,
        DateTime FechaFin,
        decimal PrecioEstimado,
        IReadOnlyCollection<CitaCampoValorRequest> CamposValor,
        int? CurrentIdCita)
    {
        public DateTime FechaInicio { get; init; }
        public int? IdPrestador { get; init; }

    }

    private sealed record AssignedAgendaCitaContext(
        int IdNegocio,
        int IdCita,
        int IdPrestador);
}
