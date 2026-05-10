using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Application.Notificaciones;
using TuCita.Application.Pagos;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Pagos;

internal sealed class PagoFlowService(
    ReservaFlowDbContext dbContext,
    IFlowClient flowClient,
    IOptions<FlowOptions> flowOptions,
    INotificacionService notificacionService,
    IAuditoriaService auditoriaService) : IPagoFlowService
{
    private const string ProviderName = "Flow";
    private const string ManualProviderName = "Manual";
    private const string FlowPaymentMethodName = "Flow";
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";
    private const string RecepcionistaRoleName = "Recepcionista";
    private const string EstadoPagoPendienteName = "Pendiente";
    private const string EstadoPagoPagadoName = "Pagado";
    private const string EstadoPagoParcialmenteDevueltoName = "Parcialmente devuelto";
    private const string EstadoPagoDevueltoName = "Devuelto";
    private const string EstadoPagoRechazadoName = "Rechazado";
    private const string EstadoPagoAnuladoName = "Anulado";
    private const string EstadoPagoErrorName = "Error";
    private const string EstadoCitaPendienteName = "Pendiente";
    private const string EstadoCitaConfirmadaName = "Confirmada";
    private const string EstadoCitaPendientePagoName = "Pendiente de pago";
    private const string EstadoCitaCanceladaName = "Cancelada";
    private static readonly CurrentUserContext SystemUser = new(string.Empty, ["Sistema"]);
    private static readonly CultureInfo ChileCulture = CultureInfo.GetCultureInfo("es-CL");

    public async Task<ServiceResult<CrearPagoFlowResponseDto>> CrearPagoReservaPublicaAsync(
        CurrentUserContext currentUser,
        string slug,
        string codigo,
        CrearPagoReservaPublicaRequest request,
        CancellationToken cancellationToken)
    {
        var configurationErrors = ValidateFlowConfiguration();
        if (configurationErrors.Count > 0)
        {
            return ServiceResult<CrearPagoFlowResponseDto>.Validation(configurationErrors);
        }

        var validationErrors = DataAnnotationsValidator.Validate(request).ToList();
        if (validationErrors.Count > 0)
        {
            return ServiceResult<CrearPagoFlowResponseDto>.Validation(validationErrors);
        }

        var cita = await GetCitaByCodigoAsync(slug, codigo, cancellationToken);
        if (cita is null)
        {
            return ServiceResult<CrearPagoFlowResponseDto>.NotFound("La reserva no existe para este negocio.");
        }

        if (!await CanCreatePaymentAsync(currentUser, cita, request, cancellationToken))
        {
            return ServiceResult<CrearPagoFlowResponseDto>.Forbidden("No tienes acceso para pagar esta reserva.");
        }

        if (!cita.Servicio.RequierePagoAnticipado)
        {
            return ServiceResult<CrearPagoFlowResponseDto>.Validation([
                new ValidationError(string.Empty, "El servicio de esta reserva no requiere pago anticipado.")
            ]);
        }

        if (cita.PrecioEstimado <= 0)
        {
            return ServiceResult<CrearPagoFlowResponseDto>.Validation([
                new ValidationError(nameof(cita.PrecioEstimado), "La reserva no tiene un monto válido para pagar.")
            ]);
        }

        var payerEmail = GetPayerEmail(cita, request);
        if (string.IsNullOrWhiteSpace(payerEmail))
        {
            return ServiceResult<CrearPagoFlowResponseDto>.Validation([
                new ValidationError(nameof(request.Email), "Debes indicar un email para generar el pago.")
            ]);
        }

        var saldoPendiente = GetSaldoPendiente(cita);
        if (saldoPendiente <= 0)
        {
            return ServiceResult<CrearPagoFlowResponseDto>.Validation([
                new ValidationError(string.Empty, "La reserva ya se encuentra pagada.")
            ]);
        }

        var existingPending = cita.Pagos
            .Where(pago =>
                pago.EstadoPago.Nombre == EstadoPagoPendienteName &&
                !string.IsNullOrWhiteSpace(pago.CheckoutUrl) &&
                pago.Monto == saldoPendiente &&
                (!pago.FechaExpiracion.HasValue || pago.FechaExpiracion > DateTime.Now))
            .OrderByDescending(pago => pago.FechaCreacion)
            .FirstOrDefault();

        if (existingPending is not null)
        {
            return ServiceResult<CrearPagoFlowResponseDto>.Success(ToCreateResponse(existingPending));
        }

        var estadoPendiente = await GetEstadoPagoAsync(EstadoPagoPendienteName, cancellationToken);
        if (estadoPendiente is null)
        {
            return ServiceResult<CrearPagoFlowResponseDto>.Validation([
                new ValidationError(string.Empty, "No existe un estado de pago Pendiente activo.")
            ]);
        }

        var metodoFlow = await GetMetodoPagoAsync(FlowPaymentMethodName, requireManual: false, cancellationToken);
        if (metodoFlow is null)
        {
            return ServiceResult<CrearPagoFlowResponseDto>.Validation([
                new ValidationError(string.Empty, "No existe un método de pago Flow activo.")
            ]);
        }

        await EnsureCitaPendientePagoAsync(cita, cancellationToken);

        var payment = new Pago
        {
            IdNegocio = cita.IdNegocio,
            IdCita = cita.IdCita,
            IdEstadoPago = estadoPendiente.IdEstadoPago,
            EstadoPago = estadoPendiente,
            IdMetodoPago = metodoFlow.IdMetodoPago,
            MetodoPago = metodoFlow,
            Cita = cita,
            Negocio = cita.Negocio,
            Proveedor = ProviderName,
            EsManual = false,
            CommerceOrder = GenerateCommerceOrder(cita),
            Monto = saldoPendiente,
            Moneda = flowOptions.Value.Currency,
            Subject = BuildSubject(cita),
            PayerEmail = payerEmail,
            PaymentMethod = flowOptions.Value.PaymentMethod,
            FechaExpiracion = flowOptions.Value.TimeoutSeconds > 0
                ? DateTime.Now.AddSeconds(flowOptions.Value.TimeoutSeconds)
                : null
        };

        dbContext.Pagos.Add(payment);
        AddPagoHistorial(
            payment,
            "CreacionFlow",
            null,
            EstadoPagoPendienteName,
            payment.Monto,
            "Orden de pago Flow creada.",
            payment.CommerceOrder,
            currentUser.IsAuthenticated ? currentUser.UserId : null);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                payment.IdNegocio,
                "Pagos",
                "CrearFlow",
                nameof(Pago),
                payment.IdPago.ToString(),
                $"Orden de pago Flow creada para la cita {cita.Codigo}.",
                ValoresNuevos: ToAuditSnapshot(payment)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var optionalJson = JsonSerializer.Serialize(new
            {
                payment.IdPago,
                cita.IdCita,
                cita.Codigo,
                cita.IdNegocio
            });

            var flowResponse = await flowClient.CreatePaymentAsync(
                new FlowCreatePaymentRequest(
                    payment.CommerceOrder,
                    payment.Subject ?? $"Reserva {cita.Codigo}",
                    payment.Monto,
                    payment.PayerEmail ?? string.Empty,
                    optionalJson),
                cancellationToken);

            var previousSnapshot = ToAuditSnapshot(payment);
            payment.FlowOrder = flowResponse.FlowOrder;
            payment.Token = flowResponse.Token;
            payment.CheckoutUrl = $"{flowResponse.Url}?token={flowResponse.Token}";
            payment.RawCreateResponseJson = JsonSerializer.Serialize(flowResponse);
            payment.FechaActualizacion = DateTime.Now;
            payment.Error = null;

            await auditoriaService.RegistrarAsync(
                currentUser,
                new AuditoriaRegistro(
                    payment.IdNegocio,
                    "Pagos",
                    "ActualizarCheckoutFlow",
                    nameof(Pago),
                    payment.IdPago.ToString(),
                    $"Checkout Flow generado para la cita {cita.Codigo}.",
                    previousSnapshot,
                    ToAuditSnapshot(payment),
                    new { payment.CommerceOrder, payment.FlowOrder }),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            var created = await GetPagoByIdQuery(payment.IdPago).FirstAsync(cancellationToken);
            return ServiceResult<CrearPagoFlowResponseDto>.Success(ToCreateResponse(created));
        }
        catch (Exception exception) when (exception is FlowClientException or HttpRequestException or TaskCanceledException)
        {
            var previousSnapshot = ToAuditSnapshot(payment);
            var estadoError = await GetEstadoPagoAsync(EstadoPagoErrorName, cancellationToken);
            if (estadoError is not null)
            {
                AddPagoHistorial(
                    payment,
                    "ErrorFlow",
                    EstadoPagoPendienteName,
                    estadoError.Nombre,
                    payment.Monto,
                    TrimToMax(exception.Message, 500),
                    payment.CommerceOrder,
                    currentUser.IsAuthenticated ? currentUser.UserId : null);
                payment.IdEstadoPago = estadoError.IdEstadoPago;
            }

            payment.Error = TrimToMax(exception.Message, 1000);
            payment.FechaActualizacion = DateTime.Now;
            await auditoriaService.RegistrarAsync(
                currentUser,
                new AuditoriaRegistro(
                    payment.IdNegocio,
                    "Pagos",
                    "ErrorFlow",
                    nameof(Pago),
                    payment.IdPago.ToString(),
                    $"Flow rechazó la creación del pago para la cita {cita.Codigo}.",
                    previousSnapshot,
                    ToAuditSnapshot(payment),
                    new { Error = payment.Error }),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult<CrearPagoFlowResponseDto>.Validation([
                new ValidationError(string.Empty, "No se pudo crear la orden de pago en Flow."),
                new ValidationError("Flow", payment.Error)
            ]);
        }
    }

    public Task<ServiceResult<PagoFlowResultadoDto>> ConfirmarPagoAsync(
        string token,
        CancellationToken cancellationToken)
    {
        return ConsultarYActualizarAsync(token, includeRedirectUrl: false, cancellationToken);
    }

    public Task<ServiceResult<PagoFlowResultadoDto>> ProcesarRetornoAsync(
        string token,
        CancellationToken cancellationToken)
    {
        return ConsultarYActualizarAsync(token, includeRedirectUrl: true, cancellationToken);
    }

    public async Task<ServiceResult<PagoFlowDto>> GetByCommerceOrderAsync(
        string commerceOrder,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(commerceOrder))
        {
            return ServiceResult<PagoFlowDto>.Validation([
                new ValidationError(nameof(commerceOrder), "La orden de comercio es obligatoria.")
            ]);
        }

        var pago = await BasePagoQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.CommerceOrder == commerceOrder.Trim(), cancellationToken);

        return pago is null
            ? ServiceResult<PagoFlowDto>.NotFound("El pago no existe.")
            : ServiceResult<PagoFlowDto>.Success(ToDto(pago));
    }

    public async Task<ServiceResult<PagoNegocioListadoDto>> GetPagosNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PagoQuery query,
        CancellationToken cancellationToken)
    {
        if (!await CanViewBusinessPaymentsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<PagoNegocioListadoDto>.Forbidden("No tienes acceso para ver los pagos de este negocio.");
        }

        var pagosQuery = ApplyPagoFilters(
            BasePagoQuery()
                .AsNoTracking()
                .Where(pago => pago.IdNegocio == idNegocio),
            query);

        var totalItems = await pagosQuery.CountAsync(cancellationToken);
        var resumen = await BuildPagoResumenAsync(pagosQuery, cancellationToken);

        var pagos = await pagosQuery
            .OrderByDescending(pago => pago.FechaCreacion)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        var pagedResult = new PagedResult<PagoNegocioDto>(
            pagos.Select(ToNegocioDto).ToArray(),
            query.PageNumber,
            query.PageSize,
            totalItems);

        return ServiceResult<PagoNegocioListadoDto>.Success(new PagoNegocioListadoDto(resumen, pagedResult));
    }

    public async Task<ServiceResult<PagoNegocioDto>> RegistrarPagoManualAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        RegistrarPagoManualRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanManageBusinessPaymentsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<PagoNegocioDto>.Forbidden("No tienes acceso para registrar pagos de este negocio.");
        }

        var validationErrors = DataAnnotationsValidator.Validate(request).ToList();
        if (validationErrors.Count > 0)
        {
            return ServiceResult<PagoNegocioDto>.Validation(validationErrors);
        }

        if (request.FechaPago.HasValue && request.FechaPago.Value > DateTime.Now)
        {
            return ServiceResult<PagoNegocioDto>.Validation([
                new ValidationError(nameof(request.FechaPago), "La fecha de pago no puede ser futura.")
            ]);
        }

        var metodoPago = await GetMetodoPagoAsync(request.IdMetodoPago, requireManual: true, cancellationToken);
        if (metodoPago is null)
        {
            return ServiceResult<PagoNegocioDto>.Validation([
                new ValidationError(
                    nameof(request.IdMetodoPago),
                    "El método de pago manual no existe o no está activo.")
            ]);
        }

        var cita = await dbContext.Citas
            .Include(item => item.Negocio)
            .Include(item => item.Cliente)
            .Include(item => item.Servicio)
            .Include(item => item.EstadoCita)
            .Include(item => item.Historial)
            .Include(item => item.Pagos)
                .ThenInclude(pago => pago.EstadoPago)
            .FirstOrDefaultAsync(
                item => item.IdNegocio == idNegocio && item.IdCita == request.IdCita,
                cancellationToken);

        if (cita is null)
        {
            return ServiceResult<PagoNegocioDto>.NotFound("La cita no existe para este negocio.");
        }

        if (cita.PrecioEstimado <= 0)
        {
            return ServiceResult<PagoNegocioDto>.Validation([
                new ValidationError(nameof(request.Monto), "La cita no tiene un monto estimado válido para registrar pago.")
            ]);
        }

        var saldoPendiente = GetSaldoPendiente(cita);
        if (saldoPendiente <= 0)
        {
            return ServiceResult<PagoNegocioDto>.Validation([
                new ValidationError(nameof(request.Monto), "La cita ya se encuentra pagada.")
            ]);
        }

        if (request.Monto > saldoPendiente)
        {
            return ServiceResult<PagoNegocioDto>.Validation([
                new ValidationError(nameof(request.Monto), $"El monto no puede superar el saldo pendiente de {saldoPendiente:N0} {flowOptions.Value.Currency}.")
            ]);
        }

        var estadoPagado = await GetEstadoPagoAsync(EstadoPagoPagadoName, cancellationToken);
        if (estadoPagado is null)
        {
            return ServiceResult<PagoNegocioDto>.Validation([
                new ValidationError(string.Empty, "No existe un estado de pago Pagado activo.")
            ]);
        }

        var now = DateTime.Now;
        var fechaPago = request.FechaPago ?? now;
        var pago = new Pago
        {
            IdNegocio = cita.IdNegocio,
            IdCita = cita.IdCita,
            IdEstadoPago = estadoPagado.IdEstadoPago,
            EstadoPago = estadoPagado,
            IdMetodoPago = metodoPago.IdMetodoPago,
            MetodoPago = metodoPago,
            Cita = cita,
            Negocio = cita.Negocio,
            RegistradoPorUserId = currentUser.UserId,
            Proveedor = ManualProviderName,
            EsManual = true,
            CommerceOrder = GenerateManualCommerceOrder(cita),
            Monto = request.Monto,
            Moneda = flowOptions.Value.Currency,
            Subject = $"Pago manual {metodoPago.Nombre} - {cita.Codigo}",
            PayerEmail = cita.Cliente.Email,
            ReferenciaManual = NormalizeOptional(request.Referencia, 100),
            ObservacionManual = NormalizeOptional(request.Observacion, 500),
            FechaPago = fechaPago,
            FechaRegistroManual = now,
            FechaActualizacion = now
        };

        dbContext.Pagos.Add(pago);
        AddPagoHistorial(
            pago,
            "CreacionManual",
            null,
            EstadoPagoPagadoName,
            pago.Monto,
            request.Observacion ?? "Pago manual registrado.",
            request.Referencia,
            currentUser.UserId);
        var estadoNotificacion = await ApplyPostManualPaymentStateAsync(cita, pago, currentUser.UserId, request.Monto == saldoPendiente, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                pago.IdNegocio,
                "Pagos",
                "RegistrarManual",
                nameof(Pago),
                pago.IdPago.ToString(),
                $"Pago manual registrado para la cita {cita.Codigo}.",
                ValoresNuevos: ToAuditSnapshot(pago),
                Metadata: new { request.Referencia, request.Observacion }),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(estadoNotificacion))
        {
            await notificacionService.CrearPorCambioEstadoAsync(cita.IdCita, estadoNotificacion, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var created = await GetPagoByIdQuery(pago.IdPago)
            .AsNoTracking()
            .FirstAsync(cancellationToken);

        return ServiceResult<PagoNegocioDto>.Success(ToNegocioDto(created));
    }

    public async Task<ServiceResult<PagoNegocioDto>> AnularPagoManualAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPago,
        AnularPagoManualRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanManageBusinessPaymentsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<PagoNegocioDto>.Forbidden("No tienes acceso para anular pagos de este negocio.");
        }

        var validationErrors = DataAnnotationsValidator.Validate(request).ToList();
        if (validationErrors.Count > 0)
        {
            return ServiceResult<PagoNegocioDto>.Validation(validationErrors);
        }

        var pago = await BasePagoQuery(includeCitaPagos: true)
            .FirstOrDefaultAsync(item => item.IdNegocio == idNegocio && item.IdPago == idPago, cancellationToken);

        if (pago is null)
        {
            return ServiceResult<PagoNegocioDto>.NotFound("El pago no existe para este negocio.");
        }

        if (!pago.EsManual)
        {
            return ServiceResult<PagoNegocioDto>.Validation([
                new ValidationError(nameof(idPago), "Solo se pueden anular manualmente pagos registrados en el negocio.")
            ]);
        }

        if (pago.EstadoPago.Nombre == EstadoPagoAnuladoName)
        {
            return ServiceResult<PagoNegocioDto>.Validation([
                new ValidationError(nameof(idPago), "El pago ya se encuentra anulado.")
            ]);
        }

        if (pago.MontoDevuelto > 0)
        {
            return ServiceResult<PagoNegocioDto>.Validation([
                new ValidationError(nameof(idPago), "No se puede anular un pago que ya tiene devoluciones registradas.")
            ]);
        }

        var estadoAnulado = await GetEstadoPagoAsync(EstadoPagoAnuladoName, cancellationToken);
        if (estadoAnulado is null)
        {
            return ServiceResult<PagoNegocioDto>.Validation([
                new ValidationError(string.Empty, "No existe un estado de pago Anulado activo.")
            ]);
        }

        var estadoAnterior = pago.EstadoPago.Nombre;
        var previousSnapshot = ToAuditSnapshot(pago);
        var now = DateTime.Now;
        pago.IdEstadoPago = estadoAnulado.IdEstadoPago;
        pago.EstadoPago = estadoAnulado;
        pago.MotivoAnulacion = NormalizeOptional(request.Motivo, 500);
        pago.ReferenciaAnulacion = NormalizeOptional(request.Referencia, 100);
        pago.AnuladoPorUserId = currentUser.UserId;
        pago.FechaAnulacion = now;
        pago.FechaActualizacion = now;
        pago.Error = null;

        AddPagoHistorial(
            pago,
            "AnulacionManual",
            estadoAnterior,
            estadoAnulado.Nombre,
            pago.Monto,
            request.Motivo,
            request.Referencia,
            currentUser.UserId);

        await RecalcularEstadoCitaPorSaldoAsync(pago.Cita, currentUser.UserId, "Estado de cita actualizado por anulación de pago manual.", cancellationToken);
        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                pago.IdNegocio,
                "Pagos",
                "AnularManual",
                nameof(Pago),
                pago.IdPago.ToString(),
                $"Pago manual anulado para la cita {pago.Cita.Codigo}.",
                previousSnapshot,
                ToAuditSnapshot(pago),
                new { request.Motivo, request.Referencia }),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await GetPagoByIdQuery(pago.IdPago)
            .AsNoTracking()
            .FirstAsync(cancellationToken);

        return ServiceResult<PagoNegocioDto>.Success(ToNegocioDto(updated));
    }

    public async Task<ServiceResult<PagoNegocioDto>> RegistrarDevolucionAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPago,
        RegistrarDevolucionPagoRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanManageBusinessPaymentsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<PagoNegocioDto>.Forbidden("No tienes acceso para registrar devoluciones de este negocio.");
        }

        var validationErrors = DataAnnotationsValidator.Validate(request).ToList();
        if (validationErrors.Count > 0)
        {
            return ServiceResult<PagoNegocioDto>.Validation(validationErrors);
        }

        if (request.FechaDevolucion.HasValue && request.FechaDevolucion.Value > DateTime.Now)
        {
            return ServiceResult<PagoNegocioDto>.Validation([
                new ValidationError(nameof(request.FechaDevolucion), "La fecha de devolución no puede ser futura.")
            ]);
        }

        var pago = await BasePagoQuery(includeCitaPagos: true)
            .FirstOrDefaultAsync(item => item.IdNegocio == idNegocio && item.IdPago == idPago, cancellationToken);

        if (pago is null)
        {
            return ServiceResult<PagoNegocioDto>.NotFound("El pago no existe para este negocio.");
        }

        if (pago.EstadoPago.Nombre is not EstadoPagoPagadoName and not EstadoPagoParcialmenteDevueltoName)
        {
            return ServiceResult<PagoNegocioDto>.Validation([
                new ValidationError(nameof(idPago), "Solo se pueden registrar devoluciones sobre pagos pagados o parcialmente devueltos.")
            ]);
        }

        var montoDisponible = Math.Max(pago.Monto - pago.MontoDevuelto, 0m);
        if (request.Monto > montoDisponible)
        {
            return ServiceResult<PagoNegocioDto>.Validation([
                new ValidationError(nameof(request.Monto), $"El monto a devolver no puede superar el saldo disponible del pago: {montoDisponible:N0} {pago.Moneda}.")
            ]);
        }

        var estadoParcial = await GetEstadoPagoAsync(EstadoPagoParcialmenteDevueltoName, cancellationToken);
        var estadoDevuelto = await GetEstadoPagoAsync(EstadoPagoDevueltoName, cancellationToken);
        if (estadoParcial is null || estadoDevuelto is null)
        {
            return ServiceResult<PagoNegocioDto>.Validation([
                new ValidationError(string.Empty, "Faltan estados activos para registrar devoluciones.")
            ]);
        }

        var estadoAnterior = pago.EstadoPago.Nombre;
        var previousSnapshot = ToAuditSnapshot(pago);
        var nuevoMontoDevuelto = pago.MontoDevuelto + request.Monto;
        var devolucionCompleta = nuevoMontoDevuelto >= pago.Monto;
        var estadoNuevo = devolucionCompleta ? estadoDevuelto : estadoParcial;
        var now = DateTime.Now;

        pago.MontoDevuelto = Math.Min(nuevoMontoDevuelto, pago.Monto);
        pago.IdEstadoPago = estadoNuevo.IdEstadoPago;
        pago.EstadoPago = estadoNuevo;
        pago.FechaUltimaDevolucion = request.FechaDevolucion ?? now;
        pago.FechaActualizacion = now;
        pago.Error = null;

        AddPagoHistorial(
            pago,
            devolucionCompleta ? "DevoluciónTotal" : "DevoluciónParcial",
            estadoAnterior,
            estadoNuevo.Nombre,
            request.Monto,
            request.Motivo,
            request.Referencia,
            currentUser.UserId);

        await RecalcularEstadoCitaPorSaldoAsync(pago.Cita, currentUser.UserId, "Estado de cita actualizado por devolución de pago.", cancellationToken);
        await auditoriaService.RegistrarAsync(
            currentUser,
            new AuditoriaRegistro(
                pago.IdNegocio,
                "Pagos",
                devolucionCompleta ? "DevoluciónTotal" : "DevoluciónParcial",
                nameof(Pago),
                pago.IdPago.ToString(),
                $"Devolución registrada para la cita {pago.Cita.Codigo}.",
                previousSnapshot,
                ToAuditSnapshot(pago),
                new { request.Monto, request.Motivo, request.Referencia, request.FechaDevolucion }),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await GetPagoByIdQuery(pago.IdPago)
            .AsNoTracking()
            .FirstAsync(cancellationToken);

        return ServiceResult<PagoNegocioDto>.Success(ToNegocioDto(updated));
    }

    public async Task<ServiceResult<IReadOnlyCollection<EstadoPagoFiltroDto>>> GetEstadosPagoSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PagoFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!await CanViewBusinessPaymentsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<IReadOnlyCollection<EstadoPagoFiltroDto>>.Forbidden("No tienes acceso para ver los filtros de pagos de este negocio.");
        }

        var estadosQuery = dbContext.EstadosPago
            .AsNoTracking()
            .Where(estado => estado.Activo);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            estadosQuery = estadosQuery.Where(estado => estado.Nombre.Contains(search));
        }

        var estados = await estadosQuery
            .OrderBy(estado => estado.EsEstadoFinal)
            .ThenBy(estado => estado.Nombre)
            .Take(query.Take)
            .Select(estado => new EstadoPagoFiltroDto(
                estado.IdEstadoPago,
                estado.Nombre,
                estado.EsEstadoFinal,
                dbContext.Pagos.Count(pago => pago.IdNegocio == idNegocio && pago.IdEstadoPago == estado.IdEstadoPago)))
            .ToArrayAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<EstadoPagoFiltroDto>>.Success(estados);
    }

    public async Task<ServiceResult<IReadOnlyCollection<PagoProveedorFiltroDto>>> GetProveedoresSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PagoFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!await CanViewBusinessPaymentsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<IReadOnlyCollection<PagoProveedorFiltroDto>>.Forbidden("No tienes acceso para ver los filtros de pagos de este negocio.");
        }

        var proveedoresQuery = dbContext.Pagos
            .AsNoTracking()
            .Where(pago => pago.IdNegocio == idNegocio);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            proveedoresQuery = proveedoresQuery.Where(pago => pago.Proveedor.Contains(search));
        }

        var proveedoresData = await proveedoresQuery
            .GroupBy(pago => pago.Proveedor)
            .Select(group => new
            {
                Proveedor = group.Key,
                CantidadPagos = group.Count()
            })
            .OrderBy(item => item.Proveedor)
            .Take(query.Take)
            .ToArrayAsync(cancellationToken);

        var proveedores = proveedoresData
            .Select(item => new PagoProveedorFiltroDto(
                item.Proveedor,
                item.Proveedor,
                item.CantidadPagos))
            .ToArray();

        if (string.IsNullOrWhiteSpace(query.Search) &&
            proveedores.All(item => item.Value != ProviderName) &&
            proveedores.Length < query.Take)
        {
            proveedores = proveedores
                .Append(new PagoProveedorFiltroDto(ProviderName, ProviderName, 0))
                .OrderBy(item => item.Label)
                .ToArray();
        }

        if (string.IsNullOrWhiteSpace(query.Search) &&
            proveedores.All(item => item.Value != ManualProviderName) &&
            proveedores.Length < query.Take)
        {
            proveedores = proveedores
                .Append(new PagoProveedorFiltroDto(ManualProviderName, ManualProviderName, 0))
                .OrderBy(item => item.Label)
                .ToArray();
        }

        return ServiceResult<IReadOnlyCollection<PagoProveedorFiltroDto>>.Success(proveedores);
    }

    public async Task<ServiceResult<IReadOnlyCollection<PagoMetodoFiltroDto>>> GetMetodosSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PagoFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!await CanManageBusinessPaymentsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<IReadOnlyCollection<PagoMetodoFiltroDto>>.Forbidden("No tienes acceso para ver los métodos de pago de este negocio.");
        }

        var metodosQuery = dbContext.MetodosPago
            .AsNoTracking()
            .Where(metodo => metodo.Activo);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            metodosQuery = metodosQuery.Where(metodo => metodo.Nombre.Contains(search));
        }

        var metodos = await metodosQuery
            .OrderBy(metodo => metodo.EsManual)
            .ThenBy(metodo => metodo.Nombre)
            .Take(query.Take)
            .Select(metodo => new PagoMetodoFiltroDto(
                metodo.IdMetodoPago,
                metodo.Nombre,
                metodo.EsManual,
                metodo.EsOnline,
                dbContext.Pagos.Count(pago => pago.IdNegocio == idNegocio && pago.IdMetodoPago == metodo.IdMetodoPago)))
            .ToArrayAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<PagoMetodoFiltroDto>>.Success(metodos);
    }

    public async Task<ServiceResult<IReadOnlyCollection<PagoOrigenFiltroDto>>> GetOrigenesSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PagoFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!await CanViewBusinessPaymentsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<IReadOnlyCollection<PagoOrigenFiltroDto>>.Forbidden("No tienes acceso para ver los filtros de pagos de este negocio.");
        }

        var counts = await dbContext.Pagos
            .AsNoTracking()
            .Where(pago => pago.IdNegocio == idNegocio)
            .GroupBy(pago => pago.EsManual)
            .Select(group => new
            {
                EsManual = group.Key,
                CantidadPagos = group.Count()
            })
            .ToDictionaryAsync(item => item.EsManual, item => item.CantidadPagos, cancellationToken);

        IEnumerable<PagoOrigenFiltroDto> origenes =
        [
            new PagoOrigenFiltroDto(false, "Online", counts.GetValueOrDefault(false)),
            new PagoOrigenFiltroDto(true, "Manual", counts.GetValueOrDefault(true))
        ];

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            origenes = origenes.Where(origen => origen.Label.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return ServiceResult<IReadOnlyCollection<PagoOrigenFiltroDto>>.Success(
            origenes
                .Take(query.Take)
                .ToArray());
    }

    public async Task<ServiceResult<IReadOnlyCollection<PagoClienteFiltroDto>>> GetClientesSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PagoFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!await CanViewBusinessPaymentsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<IReadOnlyCollection<PagoClienteFiltroDto>>.Forbidden("No tienes acceso para ver los filtros de pagos de este negocio.");
        }

        var clientesQuery = dbContext.Pagos
            .AsNoTracking()
            .Where(pago => pago.IdNegocio == idNegocio);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            clientesQuery = clientesQuery.Where(pago =>
                pago.Cita.Cliente.Nombre.Contains(search) ||
                (pago.Cita.Cliente.Email != null && pago.Cita.Cliente.Email.Contains(search)));
        }

        var clientesData = await clientesQuery
            .GroupBy(pago => new
            {
                pago.Cita.IdCliente,
                pago.Cita.Cliente.Nombre,
                pago.Cita.Cliente.Email
            })
            .Select(group => new
            {
                group.Key.IdCliente,
                group.Key.Nombre,
                group.Key.Email,
                CantidadPagos = group.Count()
            })
            .OrderBy(item => item.Nombre)
            .Take(query.Take)
            .ToArrayAsync(cancellationToken);

        var clientes = clientesData
            .Select(item => new PagoClienteFiltroDto(
                item.IdCliente,
                item.Email == null
                    ? item.Nombre
                    : item.Nombre + " - " + item.Email,
                item.Nombre,
                item.Email,
                item.CantidadPagos))
            .ToArray();

        return ServiceResult<IReadOnlyCollection<PagoClienteFiltroDto>>.Success(clientes);
    }

    public async Task<ServiceResult<IReadOnlyCollection<PagoServicioFiltroDto>>> GetServiciosSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PagoFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!await CanViewBusinessPaymentsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<IReadOnlyCollection<PagoServicioFiltroDto>>.Forbidden("No tienes acceso para ver los filtros de pagos de este negocio.");
        }

        var serviciosQuery = dbContext.Pagos
            .AsNoTracking()
            .Where(pago => pago.IdNegocio == idNegocio);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            serviciosQuery = serviciosQuery.Where(pago => pago.Cita.Servicio.Nombre.Contains(search));
        }

        var serviciosData = await serviciosQuery
            .GroupBy(pago => new
            {
                pago.Cita.IdServicio,
                pago.Cita.Servicio.Nombre,
                pago.Cita.Servicio.RequierePagoAnticipado
            })
            .Select(group => new
            {
                group.Key.IdServicio,
                group.Key.Nombre,
                group.Key.RequierePagoAnticipado,
                CantidadPagos = group.Count()
            })
            .OrderBy(item => item.Nombre)
            .Take(query.Take)
            .ToArrayAsync(cancellationToken);

        var servicios = serviciosData
            .Select(item => new PagoServicioFiltroDto(
                item.IdServicio,
                item.Nombre,
                item.Nombre,
                item.RequierePagoAnticipado,
                item.CantidadPagos))
            .ToArray();

        return ServiceResult<IReadOnlyCollection<PagoServicioFiltroDto>>.Success(servicios);
    }

    public async Task<ServiceResult<IReadOnlyCollection<PagoCitaFiltroDto>>> GetCitasSelectAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        PagoFiltroSelectQuery query,
        CancellationToken cancellationToken)
    {
        if (!await CanViewBusinessPaymentsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<IReadOnlyCollection<PagoCitaFiltroDto>>.Forbidden("No tienes acceso para ver los filtros de pagos de este negocio.");
        }

        var citasQuery = dbContext.Pagos
            .AsNoTracking()
            .Where(pago => pago.IdNegocio == idNegocio);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            citasQuery = citasQuery.Where(pago =>
                pago.Cita.Codigo.Contains(search) ||
                pago.Cita.Cliente.Nombre.Contains(search) ||
                pago.Cita.Servicio.Nombre.Contains(search));
        }

        var citasData = await citasQuery
            .GroupBy(pago => new
            {
                pago.Cita.IdCita,
                pago.Cita.Codigo,
                Cliente = pago.Cita.Cliente.Nombre,
                Servicio = pago.Cita.Servicio.Nombre,
                pago.Cita.FechaInicio
            })
            .Select(group => new
            {
                group.Key.IdCita,
                group.Key.Codigo,
                group.Key.Cliente,
                group.Key.Servicio,
                group.Key.FechaInicio,
                CantidadPagos = group.Count()
            })
            .OrderByDescending(item => item.FechaInicio)
            .Take(query.Take)
            .ToArrayAsync(cancellationToken);

        var citas = citasData
            .Select(item => new PagoCitaFiltroDto(
                item.IdCita,
                item.Codigo + " - " + item.Cliente + " - " + item.Servicio,
                item.Codigo,
                item.Cliente,
                item.Servicio,
                item.FechaInicio,
                item.CantidadPagos))
            .ToArray();

        return ServiceResult<IReadOnlyCollection<PagoCitaFiltroDto>>.Success(citas);
    }

    public async Task<ServiceResult<PagoNegocioDto>> GetPagoNegocioByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPago,
        CancellationToken cancellationToken)
    {
        if (!await CanViewBusinessPaymentsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<PagoNegocioDto>.Forbidden("No tienes acceso para ver los pagos de este negocio.");
        }

        var pago = await BasePagoQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.IdNegocio == idNegocio && item.IdPago == idPago,
                cancellationToken);

        return pago is null
            ? ServiceResult<PagoNegocioDto>.NotFound("El pago no existe.")
            : ServiceResult<PagoNegocioDto>.Success(ToNegocioDto(pago));
    }

    public async Task<ServiceResult<IReadOnlyCollection<PagoHistorialDto>>> GetHistorialPagoAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPago,
        CancellationToken cancellationToken)
    {
        if (!await CanViewBusinessPaymentsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<IReadOnlyCollection<PagoHistorialDto>>.Forbidden("No tienes acceso para ver el historial de este pago.");
        }

        var pagoExists = await dbContext.Pagos
            .AsNoTracking()
            .AnyAsync(item => item.IdNegocio == idNegocio && item.IdPago == idPago, cancellationToken);

        if (!pagoExists)
        {
            return ServiceResult<IReadOnlyCollection<PagoHistorialDto>>.NotFound("El pago no existe.");
        }

        var historial = await dbContext.PagoHistoriales
            .AsNoTracking()
            .Include(item => item.Usuario)
            .Where(item => item.IdNegocio == idNegocio && item.IdPago == idPago)
            .OrderBy(item => item.FechaCreacion)
            .ThenBy(item => item.IdPagoHistorial)
            .Select(item => new PagoHistorialDto(
                item.IdPagoHistorial,
                item.IdPago,
                item.IdNegocio,
                item.IdCita,
                item.TipoEvento,
                item.EstadoAnterior,
                item.EstadoNuevo,
                item.Monto,
                item.Motivo,
                item.Referencia,
                item.UserId,
                item.Usuario != null ? item.Usuario.Email : null,
                item.DatosJson,
                item.FechaCreacion))
            .ToArrayAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<PagoHistorialDto>>.Success(historial);
    }

    public async Task<ServiceResult<PagoComprobanteDto>> DescargarComprobanteNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idPago,
        CancellationToken cancellationToken)
    {
        if (!await CanViewBusinessPaymentsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<PagoComprobanteDto>.Forbidden("No tienes acceso para descargar comprobantes de este negocio.");
        }

        var pago = await BasePagoQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.IdNegocio == idNegocio && item.IdPago == idPago,
                cancellationToken);

        return pago is null
            ? ServiceResult<PagoComprobanteDto>.NotFound("El pago no existe.")
            : await BuildComprobanteResultAsync(pago, cancellationToken);
    }

    public async Task<ServiceResult<CitaPagosDto>> GetPagosCitaNegocioAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCita,
        CancellationToken cancellationToken)
    {
        var cita = await CitaPagosQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.IdNegocio == idNegocio && item.IdCita == idCita,
                cancellationToken);

        if (cita is null)
        {
            return ServiceResult<CitaPagosDto>.NotFound("La cita no existe.");
        }

        if (!await CanViewBusinessPaymentsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<CitaPagosDto>.Forbidden("No tienes acceso para ver los pagos de esta cita.");
        }

        return ServiceResult<CitaPagosDto>.Success(ToCitaPagosDto(cita));
    }

    public async Task<ServiceResult<PagoComprobanteDto>> DescargarComprobanteMiCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        int idPago,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return ServiceResult<PagoComprobanteDto>.Forbidden("Debes iniciar sesión para descargar tus comprobantes.");
        }

        var pago = await BasePagoQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.IdCita == idCita && item.IdPago == idPago,
                cancellationToken);

        if (pago is null)
        {
            return ServiceResult<PagoComprobanteDto>.NotFound("El pago no existe.");
        }

        if (pago.Cita.Cliente.UserId != currentUser.UserId)
        {
            return ServiceResult<PagoComprobanteDto>.Forbidden("No tienes acceso a este comprobante.");
        }

        return await BuildComprobanteResultAsync(pago, cancellationToken);
    }

    public async Task<ServiceResult<CitaPagosDto>> GetMisPagosCitaAsync(
        CurrentUserContext currentUser,
        int idCita,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return ServiceResult<CitaPagosDto>.Forbidden("Debes iniciar sesión para ver tus pagos.");
        }

        var cita = await CitaPagosQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdCita == idCita, cancellationToken);

        if (cita is null)
        {
            return ServiceResult<CitaPagosDto>.NotFound("La cita no existe.");
        }

        if (cita.Cliente.UserId != currentUser.UserId)
        {
            return ServiceResult<CitaPagosDto>.Forbidden("No tienes acceso a los pagos de esta cita.");
        }

        return ServiceResult<CitaPagosDto>.Success(ToCitaPagosDto(cita));
    }

    public async Task<ServiceResult<PagoComprobanteDto>> DescargarComprobanteReservaPublicaAsync(
        CurrentUserContext currentUser,
        string slug,
        string codigo,
        int idPago,
        DescargarComprobantePublicoRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = DataAnnotationsValidator.Validate(request).ToList();
        if (validationErrors.Count > 0)
        {
            return ServiceResult<PagoComprobanteDto>.Validation(validationErrors);
        }

        if (!currentUser.IsAuthenticated &&
            string.IsNullOrWhiteSpace(request.Email) &&
            string.IsNullOrWhiteSpace(request.Telefono))
        {
            return ServiceResult<PagoComprobanteDto>.Validation([
                new ValidationError(string.Empty, "Debes indicar el email o teléfono usado en la reserva.")
            ]);
        }

        var normalizedSlug = slug.Trim();
        var normalizedCodigo = codigo.Trim();
        var pago = await BasePagoQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item =>
                    item.IdPago == idPago &&
                    item.Negocio.Slug == normalizedSlug &&
                    item.Negocio.Activo &&
                    item.Cita.Codigo == normalizedCodigo,
                cancellationToken);

        if (pago is null)
        {
            return ServiceResult<PagoComprobanteDto>.NotFound("El pago no existe para esta reserva.");
        }

        var canView = await CanCreatePaymentAsync(
            currentUser,
            pago.Cita,
            new CrearPagoReservaPublicaRequest(request.Email, request.Telefono),
            cancellationToken);

        if (!canView)
        {
            return ServiceResult<PagoComprobanteDto>.Forbidden("No tienes acceso a este comprobante.");
        }

        return await BuildComprobanteResultAsync(pago, cancellationToken);
    }

    public async Task<ServiceResult<ProcesarPagosFlowPendientesResultDto>> ProcesarPendientesFlowAsync(
        int maxPagos,
        CancellationToken cancellationToken)
    {
        var configurationErrors = ValidateFlowConfiguration();
        if (configurationErrors.Count > 0)
        {
            return ServiceResult<ProcesarPagosFlowPendientesResultDto>.Validation(configurationErrors);
        }

        var estadoPendiente = await GetEstadoPagoAsync(EstadoPagoPendienteName, cancellationToken);
        if (estadoPendiente is null)
        {
            return ServiceResult<ProcesarPagosFlowPendientesResultDto>.Validation([
                new ValidationError(string.Empty, "No existe un estado de pago Pendiente activo.")
            ]);
        }

        var limit = Math.Clamp(maxPagos, 1, 500);
        var tokens = await dbContext.Pagos
            .AsNoTracking()
            .Where(pago =>
                !pago.EsManual &&
                pago.Proveedor == ProviderName &&
                pago.IdEstadoPago == estadoPendiente.IdEstadoPago &&
                pago.Token != null)
            .OrderBy(pago => pago.FechaUltimaConsulta ?? pago.FechaCreacion)
            .ThenBy(pago => pago.IdPago)
            .Take(limit)
            .Select(pago => pago.Token!)
            .ToArrayAsync(cancellationToken);

        var exitosos = 0;
        var conError = 0;

        foreach (var token in tokens)
        {
            var result = await ConsultarYActualizarAsync(token, includeRedirectUrl: false, cancellationToken);
            if (result.Succeeded)
            {
                exitosos++;
            }
            else
            {
                conError++;
            }
        }

        return ServiceResult<ProcesarPagosFlowPendientesResultDto>.Success(
            new ProcesarPagosFlowPendientesResultDto(tokens.Length, exitosos, conError));
    }

    public async Task<ServiceResult<ExpirarPagosPendientesResultDto>> ExpirarPendientesAsync(
        CancellationToken cancellationToken)
    {
        var estadoPendiente = await GetEstadoPagoAsync(EstadoPagoPendienteName, cancellationToken);
        var estadoAnulado = await GetEstadoPagoAsync(EstadoPagoAnuladoName, cancellationToken);
        var estadoCitaCancelada = await dbContext.EstadosCita
            .FirstOrDefaultAsync(estado => estado.Nombre == EstadoCitaCanceladaName && estado.Activo, cancellationToken);

        if (estadoPendiente is null || estadoAnulado is null || estadoCitaCancelada is null)
        {
            return ServiceResult<ExpirarPagosPendientesResultDto>.Validation([
                new ValidationError(string.Empty, "Faltan estados activos para expirar pagos pendientes.")
            ]);
        }

        var now = DateTime.Now;
        var pagosExpirados = await dbContext.Pagos
            .Include(pago => pago.Cita)
                .ThenInclude(cita => cita.EstadoCita)
            .Include(pago => pago.Cita)
                .ThenInclude(cita => cita.Servicio)
            .Include(pago => pago.Cita)
                .ThenInclude(cita => cita.Historial)
            .Include(pago => pago.Cita)
                .ThenInclude(cita => cita.Pagos)
                    .ThenInclude(citaPago => citaPago.EstadoPago)
            .Where(pago =>
                !pago.EsManual &&
                pago.Proveedor == ProviderName &&
                pago.IdEstadoPago == estadoPendiente.IdEstadoPago &&
                pago.FechaExpiracion.HasValue &&
                pago.FechaExpiracion <= now)
            .ToArrayAsync(cancellationToken);

        foreach (var pago in pagosExpirados)
        {
            var previousSnapshot = ToAuditSnapshot(pago);
            pago.IdEstadoPago = estadoAnulado.IdEstadoPago;
            pago.EstadoPago = estadoAnulado;
            pago.FechaActualizacion = now;
            pago.FechaUltimaConsulta = now;
            pago.Error = "Pago expirado automáticamente por no completarse dentro del plazo configurado.";
            AddPagoHistorial(
                pago,
                "ExpiracionAutomatica",
                EstadoPagoPendienteName,
                estadoAnulado.Nombre,
                pago.Monto,
                pago.Error,
                pago.CommerceOrder,
                null);
            await auditoriaService.RegistrarAsync(
                SystemUser,
                new AuditoriaRegistro(
                    pago.IdNegocio,
                    "Pagos",
                    "ExpirarAutomaticamente",
                    nameof(Pago),
                    pago.IdPago.ToString(),
                    $"Pago expirado automáticamente para la cita {pago.Cita.Codigo}.",
                    previousSnapshot,
                    ToAuditSnapshot(pago),
                    new { pago.FechaExpiracion, pago.CommerceOrder }),
                cancellationToken);
        }

        var citasCanceladas = new List<int>();
        foreach (var cita in pagosExpirados.Select(pago => pago.Cita).DistinctBy(cita => cita.IdCita))
        {
            var saldoCubierto = GetTotalPagadoNeto(cita.Pagos) >= cita.PrecioEstimado;
            var tienePagoPendienteVigente = cita.Pagos.Any(pago =>
                pago.IdEstadoPago == estadoPendiente.IdEstadoPago &&
                (!pago.FechaExpiracion.HasValue || pago.FechaExpiracion > now));

            if (!cita.Servicio.RequierePagoAnticipado ||
                !cita.EstadoCita.Nombre.Equals(EstadoCitaPendientePagoName, StringComparison.OrdinalIgnoreCase) ||
                saldoCubierto ||
                tienePagoPendienteVigente)
            {
                continue;
            }

            var estadoAnterior = cita.IdEstadoCita;
            cita.IdEstadoCita = estadoCitaCancelada.IdEstadoCita;
            cita.FechaActualizacion = now;
            cita.Historial.Add(new CitaHistorial
            {
                IdNegocio = cita.IdNegocio,
                IdEstadoAnterior = estadoAnterior,
                IdEstadoNuevo = estadoCitaCancelada.IdEstadoCita,
                Observacion = "Cita cancelada automáticamente por expiración del pago anticipado."
            });
            citasCanceladas.Add(cita.IdCita);
        }

        if (pagosExpirados.Length > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var idCita in citasCanceladas)
        {
            await notificacionService.CrearPorCambioEstadoAsync(idCita, EstadoCitaCanceladaName, cancellationToken);
        }

        if (citasCanceladas.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult<ExpirarPagosPendientesResultDto>.Success(
            new ExpirarPagosPendientesResultDto(pagosExpirados.Length, citasCanceladas.Count));
    }

    private async Task<ServiceResult<PagoFlowResultadoDto>> ConsultarYActualizarAsync(
        string token,
        bool includeRedirectUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ServiceResult<PagoFlowResultadoDto>.Validation([
                new ValidationError(nameof(token), "El token de Flow es obligatorio.")
            ]);
        }

        var configurationErrors = ValidateFlowConfiguration();
        if (configurationErrors.Count > 0)
        {
            return ServiceResult<PagoFlowResultadoDto>.Validation(configurationErrors);
        }

        FlowPaymentStatusResponse flowStatus;
        try
        {
            flowStatus = await flowClient.GetStatusAsync(token.Trim(), cancellationToken);
        }
        catch (Exception exception) when (exception is FlowClientException or HttpRequestException or TaskCanceledException)
        {
            return ServiceResult<PagoFlowResultadoDto>.Validation([
                new ValidationError("Flow", TrimToMax(exception.Message, 1000))
            ]);
        }

        var pago = await BasePagoQuery()
            .FirstOrDefaultAsync(
                item => item.Token == token.Trim() || item.CommerceOrder == flowStatus.CommerceOrder,
                cancellationToken);

        if (pago is null)
        {
            return ServiceResult<PagoFlowResultadoDto>.NotFound("No existe un pago local asociado al token recibido.");
        }

        await ApplyFlowStatusAsync(pago, flowStatus, cancellationToken);

        var updated = await GetPagoByIdQuery(pago.IdPago)
            .AsNoTracking()
            .FirstAsync(cancellationToken);

        return ServiceResult<PagoFlowResultadoDto>.Success(ToResultado(updated, includeRedirectUrl));
    }

    private async Task ApplyFlowStatusAsync(
        Pago pago,
        FlowPaymentStatusResponse flowStatus,
        CancellationToken cancellationToken)
    {
        var estadoName = MapEstadoPago(flowStatus.Status);
        var estadoPago = await GetEstadoPagoAsync(estadoName, cancellationToken)
            ?? await GetEstadoPagoAsync(EstadoPagoErrorName, cancellationToken);
        var estadoAnteriorPago = pago.EstadoPago.Nombre;
        var previousSnapshot = ToAuditSnapshot(pago);

        if (estadoPago is not null)
        {
            pago.IdEstadoPago = estadoPago.IdEstadoPago;
            pago.EstadoPago = estadoPago;
        }

        pago.FlowOrder = flowStatus.FlowOrder;
        pago.FlowStatus = flowStatus.Status;
        pago.FlowStatusNombre = MapFlowStatusName(flowStatus.Status);
        pago.PaymentDataJson = flowStatus.PaymentData.HasValue ? flowStatus.PaymentData.Value.GetRawText() : null;
        pago.RawStatusResponseJson = JsonSerializer.Serialize(flowStatus);
        pago.FechaUltimaConsulta = DateTime.Now;
        pago.FechaActualizacion = DateTime.Now;
        pago.Error = null;

        if (estadoPago is not null && !estadoAnteriorPago.Equals(estadoPago.Nombre, StringComparison.OrdinalIgnoreCase))
        {
            AddPagoHistorial(
                pago,
                "ActualizacionFlow",
                estadoAnteriorPago,
                estadoPago.Nombre,
                pago.Monto,
                $"Flow informó estado {MapFlowStatusName(flowStatus.Status)}.",
                pago.CommerceOrder,
                null,
                pago.RawStatusResponseJson);
            await auditoriaService.RegistrarAsync(
                SystemUser,
                new AuditoriaRegistro(
                    pago.IdNegocio,
                    "Pagos",
                    "ActualizarFlow",
                    nameof(Pago),
                    pago.IdPago.ToString(),
                    $"Flow actualizó el pago de la cita {pago.Cita.Codigo}: {estadoAnteriorPago} -> {estadoPago.Nombre}.",
                    previousSnapshot,
                    ToAuditSnapshot(pago),
                    new
                    {
                        pago.CommerceOrder,
                        pago.FlowOrder,
                        FlowStatus = flowStatus.Status,
                        FlowStatusNombre = MapFlowStatusName(flowStatus.Status)
                    }),
                cancellationToken);
        }

        var estadoAnteriorCita = pago.Cita.IdEstadoCita;
        string? estadoNuevoNombre = null;

        if (flowStatus.Status == 2)
        {
            pago.FechaPago ??= DateTime.Now;
            estadoNuevoNombre = await GetEstadoCitaPostPagoAsync(pago.IdNegocio, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(estadoNuevoNombre))
        {
            var estadoCita = await dbContext.EstadosCita
                .FirstOrDefaultAsync(estado => estado.Nombre == estadoNuevoNombre && estado.Activo, cancellationToken);

            if (estadoCita is not null && pago.Cita.IdEstadoCita != estadoCita.IdEstadoCita)
            {
                pago.Cita.IdEstadoCita = estadoCita.IdEstadoCita;
                pago.Cita.FechaActualizacion = DateTime.Now;
                pago.Cita.Historial.Add(new CitaHistorial
                {
                    IdNegocio = pago.IdNegocio,
                    IdEstadoAnterior = estadoAnteriorCita,
                    IdEstadoNuevo = estadoCita.IdEstadoCita,
                    Observacion = $"Pago confirmado por Flow. CommerceOrder: {pago.CommerceOrder}."
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (estadoAnteriorCita != pago.Cita.IdEstadoCita && !string.IsNullOrWhiteSpace(estadoNuevoNombre))
        {
            await notificacionService.CrearPorCambioEstadoAsync(pago.IdCita, estadoNuevoNombre, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureCitaPendientePagoAsync(Cita cita, CancellationToken cancellationToken)
    {
        var estadoPendientePago = await dbContext.EstadosCita
            .FirstOrDefaultAsync(estado => estado.Nombre == EstadoCitaPendientePagoName && estado.Activo, cancellationToken);

        if (estadoPendientePago is null || cita.IdEstadoCita == estadoPendientePago.IdEstadoCita)
        {
            return;
        }

        var estadoAnterior = cita.IdEstadoCita;
        cita.IdEstadoCita = estadoPendientePago.IdEstadoCita;
        cita.FechaActualizacion = DateTime.Now;
        cita.Historial.Add(new CitaHistorial
        {
            IdNegocio = cita.IdNegocio,
            IdEstadoAnterior = estadoAnterior,
            IdEstadoNuevo = estadoPendientePago.IdEstadoCita,
            Observacion = "Cita marcada como pendiente de pago al crear orden Flow."
        });
    }

    private async Task<string> GetEstadoCitaPostPagoAsync(int idNegocio, CancellationToken cancellationToken)
    {
        var requiereConfirmacionManual = await dbContext.ReglasReserva
            .AsNoTracking()
            .Where(regla => regla.IdNegocio == idNegocio)
            .Select(regla => regla.RequiereConfirmacionManual)
            .FirstOrDefaultAsync(cancellationToken);

        return requiereConfirmacionManual ? EstadoCitaPendienteName : EstadoCitaConfirmadaName;
    }

    private async Task<Cita?> GetCitaByCodigoAsync(string slug, string codigo, CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim();
        var normalizedCodigo = codigo.Trim();

        return await dbContext.Citas
            .Include(item => item.Negocio)
            .Include(item => item.Cliente)
            .Include(item => item.Servicio)
            .Include(item => item.EstadoCita)
            .Include(item => item.Historial)
            .Include(item => item.Pagos)
                .ThenInclude(pago => pago.EstadoPago)
            .FirstOrDefaultAsync(
                item =>
                    item.Negocio.Slug == normalizedSlug &&
                    item.Negocio.Activo &&
                    item.Codigo == normalizedCodigo,
                cancellationToken);
    }

    private IQueryable<Pago> BasePagoQuery(bool includeCitaPagos = false)
    {
        IQueryable<Pago> query = dbContext.Pagos
            .Include(item => item.Negocio)
            .Include(item => item.Cita)
                .ThenInclude(cita => cita.Cliente)
            .Include(item => item.Cita)
                .ThenInclude(cita => cita.Servicio)
            .Include(item => item.Cita)
                .ThenInclude(cita => cita.Prestador)
            .Include(item => item.Cita)
                .ThenInclude(cita => cita.EstadoCita)
            .Include(item => item.Cita)
                .ThenInclude(cita => cita.Historial)
            .Include(item => item.EstadoPago)
            .Include(item => item.MetodoPago)
            .Include(item => item.RegistradoPor)
            .Include(item => item.AnuladoPor);

        if (includeCitaPagos)
        {
            query = query
                .Include(item => item.Cita)
                    .ThenInclude(cita => cita.Pagos)
                        .ThenInclude(citaPago => citaPago.EstadoPago);
        }

        return query;
    }

    private IQueryable<Pago> GetPagoByIdQuery(int idPago)
    {
        return BasePagoQuery().Where(item => item.IdPago == idPago);
    }

    private static IQueryable<Pago> ApplyPagoFilters(IQueryable<Pago> query, PagoQuery filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(pago =>
                pago.CommerceOrder.Contains(search) ||
                pago.MetodoPago.Nombre.Contains(search) ||
                (pago.ReferenciaManual != null && pago.ReferenciaManual.Contains(search)) ||
                pago.Cita.Codigo.Contains(search) ||
                pago.Cita.Cliente.Nombre.Contains(search) ||
                (pago.Cita.Cliente.Email != null && pago.Cita.Cliente.Email.Contains(search)) ||
                pago.Cita.Servicio.Nombre.Contains(search) ||
                (pago.PayerEmail != null && pago.PayerEmail.Contains(search)));
        }

        if (filters.IdCita.HasValue)
        {
            query = query.Where(pago => pago.IdCita == filters.IdCita.Value);
        }

        if (filters.IdCliente.HasValue)
        {
            query = query.Where(pago => pago.Cita.IdCliente == filters.IdCliente.Value);
        }

        if (filters.IdServicio.HasValue)
        {
            query = query.Where(pago => pago.Cita.IdServicio == filters.IdServicio.Value);
        }

        if (filters.IdEstadoPago.HasValue)
        {
            query = query.Where(pago => pago.IdEstadoPago == filters.IdEstadoPago.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Proveedor))
        {
            var provider = filters.Proveedor.Trim();
            query = query.Where(pago => pago.Proveedor == provider);
        }

        if (filters.IdMetodoPago.HasValue)
        {
            query = query.Where(pago => pago.IdMetodoPago == filters.IdMetodoPago.Value);
        }

        if (filters.EsManual.HasValue)
        {
            query = query.Where(pago => pago.EsManual == filters.EsManual.Value);
        }

        if (filters.FechaDesde.HasValue)
        {
            query = query.Where(pago => pago.FechaCreacion >= filters.FechaDesde.Value);
        }

        if (filters.FechaHasta.HasValue)
        {
            query = query.Where(pago => pago.FechaCreacion <= filters.FechaHasta.Value);
        }

        if (filters.FechaPagoDesde.HasValue)
        {
            query = query.Where(pago => pago.FechaPago >= filters.FechaPagoDesde.Value);
        }

        if (filters.FechaPagoHasta.HasValue)
        {
            query = query.Where(pago => pago.FechaPago <= filters.FechaPagoHasta.Value);
        }

        if (filters.SoloPagados == true)
        {
            query = query.Where(pago =>
                pago.EstadoPago.Nombre == EstadoPagoPagadoName ||
                pago.EstadoPago.Nombre == EstadoPagoParcialmenteDevueltoName);
        }

        if (filters.SoloPendientes == true)
        {
            query = query.Where(pago => pago.EstadoPago.Nombre == EstadoPagoPendienteName);
        }

        return query;
    }

    private static async Task<PagoNegocioResumenDto> BuildPagoResumenAsync(
        IQueryable<Pago> query,
        CancellationToken cancellationToken)
    {
        var grouped = await query
            .GroupBy(pago => 1)
            .Select(group => new
            {
                CantidadTotal = group.Count(),
                CantidadPagados = group.Count(pago => pago.EstadoPago.Nombre == EstadoPagoPagadoName),
                CantidadPendientes = group.Count(pago => pago.EstadoPago.Nombre == EstadoPagoPendienteName),
                CantidadRechazados = group.Count(pago => pago.EstadoPago.Nombre == EstadoPagoRechazadoName),
                CantidadAnulados = group.Count(pago => pago.EstadoPago.Nombre == EstadoPagoAnuladoName),
                CantidadParcialmenteDevueltos = group.Count(pago => pago.EstadoPago.Nombre == EstadoPagoParcialmenteDevueltoName),
                CantidadDevueltos = group.Count(pago => pago.EstadoPago.Nombre == EstadoPagoDevueltoName),
                CantidadError = group.Count(pago => pago.EstadoPago.Nombre == EstadoPagoErrorName),
                MontoTotal = group.Sum(pago => (decimal?)pago.Monto) ?? 0m,
                MontoPagado = group.Sum(pago =>
                    pago.EstadoPago.Nombre == EstadoPagoPagadoName || pago.EstadoPago.Nombre == EstadoPagoParcialmenteDevueltoName
                        ? (decimal?)(pago.Monto - pago.MontoDevuelto)
                        : 0m) ?? 0m,
                MontoPendiente = group.Sum(pago => pago.EstadoPago.Nombre == EstadoPagoPendienteName ? (decimal?)pago.Monto : 0m) ?? 0m,
                MontoRechazado = group.Sum(pago => pago.EstadoPago.Nombre == EstadoPagoRechazadoName ? (decimal?)pago.Monto : 0m) ?? 0m,
                MontoAnulado = group.Sum(pago => pago.EstadoPago.Nombre == EstadoPagoAnuladoName ? (decimal?)pago.Monto : 0m) ?? 0m,
                MontoDevuelto = group.Sum(pago => (decimal?)pago.MontoDevuelto) ?? 0m,
                MontoError = group.Sum(pago => pago.EstadoPago.Nombre == EstadoPagoErrorName ? (decimal?)pago.Monto : 0m) ?? 0m
            })
            .FirstOrDefaultAsync(cancellationToken);

        return grouped is null
            ? new PagoNegocioResumenDto(0, 0, 0, 0, 0, 0, 0, 0, 0m, 0m, 0m, 0m, 0m, 0m, 0m)
            : new PagoNegocioResumenDto(
                grouped.CantidadTotal,
                grouped.CantidadPagados,
                grouped.CantidadPendientes,
                grouped.CantidadRechazados,
                grouped.CantidadAnulados,
                grouped.CantidadParcialmenteDevueltos,
                grouped.CantidadDevueltos,
                grouped.CantidadError,
                grouped.MontoTotal,
                grouped.MontoPagado,
                grouped.MontoPendiente,
                grouped.MontoRechazado,
                grouped.MontoAnulado,
                grouped.MontoDevuelto,
                grouped.MontoError);
    }

    private IQueryable<Cita> CitaPagosQuery()
    {
        return dbContext.Citas
            .Include(item => item.Negocio)
            .Include(item => item.Cliente)
            .Include(item => item.Servicio)
            .Include(item => item.EstadoCita)
            .Include(item => item.Pagos)
                .ThenInclude(pago => pago.EstadoPago)
            .Include(item => item.Pagos)
                .ThenInclude(pago => pago.MetodoPago)
            .Include(item => item.Pagos)
                .ThenInclude(pago => pago.RegistradoPor)
            .Include(item => item.Pagos)
                .ThenInclude(pago => pago.AnuladoPor);
    }

    private async Task<EstadoPago?> GetEstadoPagoAsync(string nombre, CancellationToken cancellationToken)
    {
        return await dbContext.EstadosPago
            .FirstOrDefaultAsync(estado => estado.Nombre == nombre && estado.Activo, cancellationToken);
    }

    private async Task<MetodoPago?> GetMetodoPagoAsync(
        string nombre,
        bool requireManual,
        CancellationToken cancellationToken)
    {
        return await dbContext.MetodosPago
            .FirstOrDefaultAsync(
                metodo =>
                    metodo.Nombre == nombre &&
                    metodo.Activo &&
                    (!requireManual || metodo.EsManual),
                cancellationToken);
    }

    private async Task<MetodoPago?> GetMetodoPagoAsync(
        int idMetodoPago,
        bool requireManual,
        CancellationToken cancellationToken)
    {
        return await dbContext.MetodosPago
            .FirstOrDefaultAsync(
                metodo =>
                    metodo.IdMetodoPago == idMetodoPago &&
                    metodo.Activo &&
                    (!requireManual || metodo.EsManual),
                cancellationToken);
    }

    private async Task<string?> ApplyPostManualPaymentStateAsync(
        Cita cita,
        Pago pago,
        string registradoPorUserId,
        bool pagoCompleto,
        CancellationToken cancellationToken)
    {
        var estadoActual = cita.IdEstadoCita;
        var observacion = $"Pago manual registrado. Método: {pago.MetodoPago.Nombre}. Monto: {pago.Monto:N0} {pago.Moneda}.";
        if (!string.IsNullOrWhiteSpace(pago.ReferenciaManual))
        {
            observacion += $" Referencia: {pago.ReferenciaManual}.";
        }

        if (!pagoCompleto)
        {
            cita.Historial.Add(new CitaHistorial
            {
                IdNegocio = cita.IdNegocio,
                IdEstadoAnterior = estadoActual,
                IdEstadoNuevo = estadoActual,
                UserId = registradoPorUserId,
                Observacion = $"{observacion} Pago parcial."
            });

            return null;
        }

        if (!cita.EstadoCita.Nombre.Equals(EstadoCitaPendientePagoName, StringComparison.OrdinalIgnoreCase))
        {
            cita.Historial.Add(new CitaHistorial
            {
                IdNegocio = cita.IdNegocio,
                IdEstadoAnterior = estadoActual,
                IdEstadoNuevo = estadoActual,
                UserId = registradoPorUserId,
                Observacion = observacion
            });

            return null;
        }

        var estadoNuevoNombre = await GetEstadoCitaPostPagoAsync(cita.IdNegocio, cancellationToken);
        var estadoCita = await dbContext.EstadosCita
            .FirstOrDefaultAsync(estado => estado.Nombre == estadoNuevoNombre && estado.Activo, cancellationToken);

        if (estadoCita is null || cita.IdEstadoCita == estadoCita.IdEstadoCita)
        {
            cita.Historial.Add(new CitaHistorial
            {
                IdNegocio = cita.IdNegocio,
                IdEstadoAnterior = estadoActual,
                IdEstadoNuevo = estadoActual,
                UserId = registradoPorUserId,
                Observacion = observacion
            });

            return null;
        }

        cita.IdEstadoCita = estadoCita.IdEstadoCita;
        cita.FechaActualizacion = DateTime.Now;
        cita.Historial.Add(new CitaHistorial
        {
            IdNegocio = cita.IdNegocio,
            IdEstadoAnterior = estadoActual,
            IdEstadoNuevo = estadoCita.IdEstadoCita,
            UserId = registradoPorUserId,
            Observacion = observacion
        });

        return estadoNuevoNombre;
    }

    private async Task<bool> CanCreatePaymentAsync(
        CurrentUserContext currentUser,
        Cita cita,
        CrearPagoReservaPublicaRequest request,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsSuperAdmin)
        {
            return true;
        }

        if (currentUser.IsAuthenticated)
        {
            if (!string.IsNullOrWhiteSpace(cita.Cliente.UserId) &&
                cita.Cliente.UserId == currentUser.UserId)
            {
                return true;
            }

            var hasBusinessAccess = await dbContext.NegocioUsuarios.AnyAsync(
                usuario =>
                    usuario.IdNegocio == cita.IdNegocio &&
                    usuario.UserId == currentUser.UserId &&
                    usuario.Activo &&
                    (usuario.RolNegocio.Nombre == OwnerRoleName ||
                        usuario.RolNegocio.Nombre == AdminRoleName ||
                        usuario.RolNegocio.Nombre == RecepcionistaRoleName),
                cancellationToken);

            if (hasBusinessAccess)
            {
                return true;
            }
        }

        return MatchesCliente(cita.Cliente, request.Email, request.Telefono);
    }

    private async Task<bool> CanManageBusinessPaymentsAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsSuperAdmin)
        {
            return true;
        }

        return currentUser.IsAuthenticated &&
            await dbContext.NegocioUsuarios.AnyAsync(
                usuario =>
                    usuario.IdNegocio == idNegocio &&
                    usuario.UserId == currentUser.UserId &&
                    usuario.Activo &&
                    (usuario.RolNegocio.Nombre == OwnerRoleName ||
                        usuario.RolNegocio.Nombre == AdminRoleName ||
                        usuario.RolNegocio.Nombre == RecepcionistaRoleName),
                cancellationToken);
    }

    private async Task<bool> CanViewBusinessPaymentsAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsSuperAdmin)
        {
            return true;
        }

        return currentUser.IsAuthenticated &&
            await dbContext.NegocioUsuarios.AnyAsync(
                usuario =>
                    usuario.IdNegocio == idNegocio &&
                    usuario.UserId == currentUser.UserId &&
                    usuario.Activo &&
                    (usuario.RolNegocio.Nombre == OwnerRoleName ||
                        usuario.RolNegocio.Nombre == AdminRoleName ||
                        usuario.RolNegocio.Nombre == RecepcionistaRoleName),
                cancellationToken);
    }

    private static bool MatchesCliente(Cliente cliente, string? email, string? telefono)
    {
        if (!string.IsNullOrWhiteSpace(email) &&
            !string.IsNullOrWhiteSpace(cliente.Email) &&
            string.Equals(cliente.Email.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(telefono) &&
            !string.IsNullOrWhiteSpace(cliente.Telefono) &&
            string.Equals(cliente.Telefono.Trim(), telefono.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyCollection<ValidationError> ValidateFlowConfiguration()
    {
        var config = flowOptions.Value;
        var errors = new List<ValidationError>();

        if (!config.Enabled)
        {
            errors.Add(new ValidationError("Flow.Enabled", "La integración con Flow está deshabilitada."));
        }

        if (string.IsNullOrWhiteSpace(config.ApiBaseUrl))
        {
            errors.Add(new ValidationError("Flow.ApiBaseUrl", "La URL base de Flow no está configurada."));
        }

        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            errors.Add(new ValidationError("Flow.ApiKey", "La API Key de Flow no está configurada."));
        }

        if (string.IsNullOrWhiteSpace(config.SecretKey))
        {
            errors.Add(new ValidationError("Flow.SecretKey", "La Secret Key de Flow no está configurada."));
        }

        if (string.IsNullOrWhiteSpace(config.UrlConfirmation))
        {
            errors.Add(new ValidationError("Flow.UrlConfirmation", "La URL de confirmación de Flow no está configurada."));
        }

        if (string.IsNullOrWhiteSpace(config.UrlReturn))
        {
            errors.Add(new ValidationError("Flow.UrlReturn", "La URL de retorno de Flow no está configurada."));
        }

        return errors;
    }

    private static string GenerateCommerceOrder(Cita cita)
    {
        var value = $"TC-{cita.IdCita}-{Guid.NewGuid():N}";
        return value.Length <= 100 ? value : value[..100];
    }

    private static string GenerateManualCommerceOrder(Cita cita)
    {
        var value = $"PM-{cita.IdCita}-{Guid.NewGuid():N}";
        return value.Length <= 100 ? value : value[..100];
    }

    private static string BuildSubject(Cita cita)
    {
        var value = $"Reserva {cita.Codigo} - {cita.Servicio.Nombre}";
        return value.Length <= 250 ? value : value[..250];
    }

    private static object ToAuditSnapshot(Pago pago)
    {
        return new
        {
            pago.IdPago,
            pago.IdNegocio,
            Negocio = pago.Negocio?.Nombre,
            pago.IdCita,
            Cita = pago.Cita?.Codigo,
            pago.IdEstadoPago,
            EstadoPago = pago.EstadoPago?.Nombre,
            pago.IdMetodoPago,
            MetodoPago = pago.MetodoPago?.Nombre,
            pago.RegistradoPorUserId,
            pago.Proveedor,
            pago.EsManual,
            pago.CommerceOrder,
            pago.FlowOrder,
            pago.CheckoutUrl,
            pago.Monto,
            pago.MontoDevuelto,
            MontoNeto = pago.EstadoPago is null ? 0m : GetMontoNeto(pago),
            pago.Moneda,
            pago.Subject,
            pago.PayerEmail,
            pago.PaymentMethod,
            pago.FlowStatus,
            pago.FlowStatusNombre,
            pago.ReferenciaManual,
            pago.ObservacionManual,
            pago.MotivoAnulacion,
            pago.ReferenciaAnulacion,
            pago.AnuladoPorUserId,
            pago.Error,
            pago.FechaCreacion,
            pago.FechaActualizacion,
            pago.FechaPago,
            pago.FechaRegistroManual,
            pago.FechaAnulacion,
            pago.FechaUltimaDevolucion,
            pago.FechaExpiracion,
            pago.FechaUltimaConsulta
        };
    }

    private static string? GetPayerEmail(Cita cita, CrearPagoReservaPublicaRequest request)
    {
        return !string.IsNullOrWhiteSpace(cita.Cliente.Email)
            ? cita.Cliente.Email.Trim()
            : request.Email?.Trim();
    }

    private static decimal GetSaldoPendiente(Cita cita)
    {
        var totalPagado = GetTotalPagadoNeto(cita.Pagos);

        return Math.Max(cita.PrecioEstimado - totalPagado, 0m);
    }

    private static decimal GetTotalPagadoNeto(IEnumerable<Pago> pagos)
    {
        return pagos
            .Where(PagoAportaSaldo)
            .Sum(GetMontoNeto);
    }

    private static decimal GetMontoNeto(Pago pago)
    {
        return PagoAportaSaldo(pago)
            ? Math.Max(pago.Monto - pago.MontoDevuelto, 0m)
            : 0m;
    }

    private static bool PagoAportaSaldo(Pago pago)
    {
        return pago.EstadoPago.Nombre is EstadoPagoPagadoName or EstadoPagoParcialmenteDevueltoName;
    }

    private async Task RecalcularEstadoCitaPorSaldoAsync(
        Cita cita,
        string? userId,
        string observacion,
        CancellationToken cancellationToken)
    {
        if (cita.EstadoCita.EsEstadoFinal ||
            !cita.Servicio.RequierePagoAnticipado ||
            cita.PrecioEstimado <= 0)
        {
            return;
        }

        var totalPagadoNeto = GetTotalPagadoNeto(cita.Pagos);
        var estadoNuevoNombre = totalPagadoNeto >= cita.PrecioEstimado
            ? await GetEstadoCitaPostPagoAsync(cita.IdNegocio, cancellationToken)
            : EstadoCitaPendientePagoName;
        var estadoNuevo = await dbContext.EstadosCita
            .FirstOrDefaultAsync(estado => estado.Nombre == estadoNuevoNombre && estado.Activo, cancellationToken);

        if (estadoNuevo is null || cita.IdEstadoCita == estadoNuevo.IdEstadoCita)
        {
            return;
        }

        var estadoAnterior = cita.IdEstadoCita;
        cita.IdEstadoCita = estadoNuevo.IdEstadoCita;
        cita.FechaActualizacion = DateTime.Now;
        cita.Historial.Add(new CitaHistorial
        {
            IdNegocio = cita.IdNegocio,
            IdEstadoAnterior = estadoAnterior,
            IdEstadoNuevo = estadoNuevo.IdEstadoCita,
            UserId = userId,
            Observacion = observacion
        });
    }

    private static void AddPagoHistorial(
        Pago pago,
        string tipoEvento,
        string? estadoAnterior,
        string? estadoNuevo,
        decimal? monto,
        string? motivo,
        string? referencia,
        string? userId,
        string? datosJson = null)
    {
        pago.Historial.Add(new PagoHistorial
        {
            IdNegocio = pago.IdNegocio,
            IdCita = pago.IdCita,
            TipoEvento = tipoEvento,
            EstadoAnterior = NormalizeOptional(estadoAnterior, 80),
            EstadoNuevo = NormalizeOptional(estadoNuevo, 80),
            Monto = monto,
            Motivo = NormalizeOptional(motivo, 500),
            Referencia = NormalizeOptional(referencia, 100),
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
            DatosJson = datosJson,
            FechaCreacion = DateTime.Now
        });
    }

    private static string MapEstadoPago(int flowStatus)
    {
        return flowStatus switch
        {
            1 => EstadoPagoPendienteName,
            2 => EstadoPagoPagadoName,
            3 => EstadoPagoRechazadoName,
            4 => EstadoPagoAnuladoName,
            _ => EstadoPagoErrorName
        };
    }

    private static string MapFlowStatusName(int flowStatus)
    {
        return flowStatus switch
        {
            1 => "Pendiente de pago",
            2 => "Pagada",
            3 => "Rechazada",
            4 => "Anulada",
            _ => "Desconocida"
        };
    }

    private CrearPagoFlowResponseDto ToCreateResponse(Pago pago)
    {
        return new CrearPagoFlowResponseDto(
            pago.IdPago,
            pago.IdCita,
            pago.Cita.Codigo,
            pago.CommerceOrder,
            pago.FlowOrder,
            pago.EstadoPago.Nombre,
            pago.Monto,
            pago.Moneda,
            pago.CheckoutUrl ?? string.Empty,
            pago.FechaExpiracion);
    }

    private PagoFlowResultadoDto ToResultado(Pago pago, bool includeRedirectUrl)
    {
        var redirectUrl = includeRedirectUrl
            ? $"{flowOptions.Value.FrontendReturnUrl.TrimEnd('?')}{(flowOptions.Value.FrontendReturnUrl.Contains('?') ? "&" : "?")}commerceOrder={Uri.EscapeDataString(pago.CommerceOrder)}"
            : null;

        return new PagoFlowResultadoDto(
            pago.CommerceOrder,
            pago.IdPago,
            pago.IdCita,
            pago.Cita.Codigo,
            pago.EstadoPago.Nombre,
            pago.FlowStatus,
            pago.FlowStatusNombre,
            pago.Monto,
            pago.Moneda,
            pago.FechaPago,
            redirectUrl);
    }

    private static PagoFlowDto ToDto(Pago pago)
    {
        return new PagoFlowDto(
            pago.IdPago,
            pago.IdNegocio,
            pago.IdCita,
            pago.Cita.Codigo,
            pago.Proveedor,
            pago.EsManual,
            pago.IdMetodoPago,
            pago.MetodoPago.Nombre,
            pago.CommerceOrder,
            pago.FlowOrder,
            pago.CheckoutUrl,
            pago.Monto,
            pago.MontoDevuelto,
            GetMontoNeto(pago),
            pago.Moneda,
            pago.EstadoPago.Nombre,
            pago.FlowStatus,
            pago.FlowStatusNombre,
            pago.FechaCreacion,
            pago.FechaActualizacion,
            pago.FechaPago,
            pago.RegistradoPorUserId,
            pago.RegistradoPor?.Email,
            pago.FechaRegistroManual,
            pago.ReferenciaManual,
            pago.ObservacionManual,
            pago.FechaAnulacion,
            pago.AnuladoPorUserId,
            pago.AnuladoPor?.Email,
            pago.MotivoAnulacion,
            pago.ReferenciaAnulacion,
            pago.FechaUltimaDevolucion,
            pago.FechaExpiracion,
            pago.Error);
    }

    private static PagoNegocioDto ToNegocioDto(Pago pago)
    {
        return new PagoNegocioDto(
            pago.IdPago,
            pago.IdNegocio,
            pago.Negocio.Nombre,
            pago.IdCita,
            pago.Cita.Codigo,
            pago.Cita.IdCliente,
            pago.Cita.Cliente.Nombre,
            pago.Cita.Cliente.Email,
            pago.Cita.IdServicio,
            pago.Cita.Servicio.Nombre,
            pago.Cita.FechaInicio,
            pago.Cita.FechaFin,
            pago.Cita.EstadoCita.Nombre,
            pago.Proveedor,
            pago.EsManual,
            pago.IdMetodoPago,
            pago.MetodoPago.Nombre,
            pago.CommerceOrder,
            pago.FlowOrder,
            pago.CheckoutUrl,
            pago.Monto,
            pago.MontoDevuelto,
            GetMontoNeto(pago),
            pago.Moneda,
            pago.IdEstadoPago,
            pago.EstadoPago.Nombre,
            pago.FlowStatus,
            pago.FlowStatusNombre,
            pago.PayerEmail,
            pago.FechaCreacion,
            pago.FechaActualizacion,
            pago.FechaPago,
            pago.RegistradoPorUserId,
            pago.RegistradoPor?.Email,
            pago.FechaRegistroManual,
            pago.ReferenciaManual,
            pago.ObservacionManual,
            pago.FechaAnulacion,
            pago.AnuladoPorUserId,
            pago.AnuladoPor?.Email,
            pago.MotivoAnulacion,
            pago.ReferenciaAnulacion,
            pago.FechaUltimaDevolucion,
            pago.FechaExpiracion,
            pago.Error);
    }

    private static CitaPagosDto ToCitaPagosDto(Cita cita)
    {
        var pagos = cita.Pagos
            .OrderByDescending(pago => pago.FechaCreacion)
            .Select(ToDto)
            .ToArray();

        var totalPagado = GetTotalPagadoNeto(cita.Pagos);

        var saldoPendiente = Math.Max(cita.PrecioEstimado - totalPagado, 0m);
        var ultimoPago = cita.Pagos
            .OrderByDescending(pago => pago.FechaActualizacion ?? pago.FechaCreacion)
            .FirstOrDefault();

        return new CitaPagosDto(
            cita.IdCita,
            cita.IdNegocio,
            cita.Negocio.Nombre,
            cita.Codigo,
            cita.IdCliente,
            cita.Cliente.Nombre,
            cita.IdServicio,
            cita.Servicio.Nombre,
            cita.IdEstadoCita,
            cita.EstadoCita.Nombre,
            cita.Servicio.RequierePagoAnticipado,
            cita.PrecioEstimado,
            totalPagado,
            saldoPendiente,
            ultimoPago?.EstadoPago.Nombre,
            cita.Pagos.Any(PagoAportaSaldo),
            cita.Pagos.Any(pago => pago.EstadoPago.Nombre == EstadoPagoPendienteName),
            pagos);
    }

    private static string TrimToMax(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static async Task<ServiceResult<PagoComprobanteDto>> BuildComprobanteResultAsync(
        Pago pago,
        CancellationToken cancellationToken)
    {
        if (!PuedeDescargarComprobante(pago))
        {
            return ServiceResult<PagoComprobanteDto>.Validation([
                new ValidationError(string.Empty, "Solo se puede descargar comprobante de pagos confirmados, devueltos o anulados.")
            ]);
        }

        var content = await PagoComprobantePdfBuilder.BuildAsync(pago, cancellationToken);
        var fileName = PagoComprobantePdfBuilder.BuildFileName(pago);

        return ServiceResult<PagoComprobanteDto>.Success(
            new PagoComprobanteDto(fileName, "application/pdf", content));
    }

    private static bool PuedeDescargarComprobante(Pago pago)
    {
        return pago.EstadoPago.Nombre.Equals(EstadoPagoPagadoName, StringComparison.OrdinalIgnoreCase) ||
            pago.EstadoPago.Nombre.Equals(EstadoPagoParcialmenteDevueltoName, StringComparison.OrdinalIgnoreCase) ||
            pago.EstadoPago.Nombre.Equals(EstadoPagoDevueltoName, StringComparison.OrdinalIgnoreCase) ||
            pago.EstadoPago.Nombre.Equals(EstadoPagoAnuladoName, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildComprobanteHtml(Pago pago)
    {
        var comprobante = $"PAG-{pago.IdPago:000000}";
        var titulo = $"Comprobante de pago {comprobante}";
        var fechaEmision = DateTime.Now;
        var neto = GetMontoNeto(pago);
        var prestador = pago.Cita.Prestador?.Nombre;

        return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{{Html(titulo)}}</title>
  <style>
    body { margin: 0; background: #f6f7fb; color: #172033; font-family: Arial, Helvetica, sans-serif; }
    .page { max-width: 780px; margin: 0 auto; padding: 32px 18px; }
    .receipt { background: #fff; border: 1px solid #e3e7ef; border-radius: 10px; overflow: hidden; }
    .bar { height: 6px; background: #2563eb; }
    .header { padding: 28px 32px 18px; display: flex; justify-content: space-between; gap: 24px; border-bottom: 1px solid #e8ecf3; }
    .brand { font-size: 13px; color: #64748b; margin-bottom: 6px; }
    h1 { margin: 0; font-size: 24px; line-height: 1.25; color: #0f172a; }
    .status { text-align: right; }
    .badge { display: inline-block; padding: 7px 10px; border-radius: 999px; background: #eff6ff; color: #1d4ed8; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: .04em; }
    .content { padding: 26px 32px 30px; }
    .summary { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 12px; margin-bottom: 24px; }
    .metric { border: 1px solid #e8ecf3; border-radius: 8px; padding: 14px; background: #fbfcff; }
    .label { color: #64748b; font-size: 12px; margin-bottom: 6px; }
    .value { color: #0f172a; font-size: 15px; font-weight: 700; overflow-wrap: anywhere; }
    .section { margin-top: 22px; }
    .section h2 { margin: 0 0 10px; font-size: 15px; color: #0f172a; }
    table { width: 100%; border-collapse: collapse; border: 1px solid #e8ecf3; border-radius: 8px; overflow: hidden; }
    td { padding: 11px 13px; border-bottom: 1px solid #e8ecf3; vertical-align: top; font-size: 14px; }
    tr:last-child td { border-bottom: 0; }
    td:first-child { width: 34%; color: #64748b; background: #f8fafc; }
    td:last-child { color: #111827; font-weight: 600; }
    .note { margin-top: 22px; color: #64748b; font-size: 12px; line-height: 1.55; }
    @media print {
      body { background: #fff; }
      .page { padding: 0; max-width: none; }
      .receipt { border: 0; border-radius: 0; }
    }
  </style>
</head>
<body>
  <main class="page">
    <article class="receipt">
      <div class="bar"></div>
      <header class="header">
        <div>
          <div class="brand">TuCita / {{Html(pago.Negocio.Nombre)}}</div>
          <h1>{{Html(titulo)}}</h1>
        </div>
        <div class="status">
          <span class="badge">{{Html(pago.EstadoPago.Nombre)}}</span>
        </div>
      </header>
      <section class="content">
        <div class="summary">
          <div class="metric">
            <div class="label">Monto pagado</div>
            <div class="value">{{Html(FormatMoney(pago.Monto, pago.Moneda))}}</div>
          </div>
          <div class="metric">
            <div class="label">Monto devuelto</div>
            <div class="value">{{Html(FormatMoney(pago.MontoDevuelto, pago.Moneda))}}</div>
          </div>
          <div class="metric">
            <div class="label">Monto neto</div>
            <div class="value">{{Html(FormatMoney(neto, pago.Moneda))}}</div>
          </div>
        </div>

        <section class="section">
          <h2>Datos del pago</h2>
          <table>
            {{Row("N° comprobante", comprobante)}}
            {{Row("Estado", pago.EstadoPago.Nombre)}}
            {{Row("Método", pago.MetodoPago.Nombre)}}
            {{Row("Proveedor", pago.Proveedor)}}
            {{Row("Orden comercio", pago.CommerceOrder)}}
            {{Row("Orden Flow", pago.FlowOrder?.ToString(ChileCulture))}}
            {{Row("Referencia manual", pago.ReferenciaManual)}}
            {{Row("Email pagador", pago.PayerEmail)}}
            {{Row("Fecha pago", FormatDate(pago.FechaPago ?? pago.FechaRegistroManual ?? pago.FechaCreacion))}}
            {{Row("Fecha emision", FormatDate(fechaEmision))}}
          </table>
        </section>

        <section class="section">
          <h2>Datos de la cita</h2>
          <table>
            {{Row("Código cita", pago.Cita.Codigo)}}
            {{Row("Cliente", pago.Cita.Cliente.Nombre)}}
            {{Row("Email cliente", pago.Cita.Cliente.Email)}}
            {{Row("Servicio", pago.Cita.Servicio.Nombre)}}
            {{Row("Prestador", prestador)}}
            {{Row("Fecha cita", FormatDate(pago.Cita.FechaInicio))}}
            {{Row("Horario", $"{pago.Cita.FechaInicio:HH:mm} - {pago.Cita.FechaFin:HH:mm}")}}
            {{Row("Estado cita", pago.Cita.EstadoCita.Nombre)}}
          </table>
        </section>

        <section class="section">
          <h2>Datos del negocio</h2>
          <table>
            {{Row("Negocio", pago.Negocio.Nombre)}}
            {{Row("Dirección", pago.Negocio.Direccion)}}
            {{Row("Teléfono", pago.Negocio.Telefono)}}
            {{Row("Email", pago.Negocio.Email)}}
          </table>
        </section>

        {{BuildAnulacionHtml(pago)}}

        <p class="note">
          Documento generado automáticamente por TuCita. Este comprobante acredita el registro operacional del pago en la plataforma y no reemplaza una boleta o factura tributaria cuando corresponda.
        </p>
      </section>
    </article>
  </main>
</body>
</html>
""";
    }

    private static string BuildAnulacionHtml(Pago pago)
    {
        if (!pago.FechaAnulacion.HasValue &&
            !pago.FechaUltimaDevolucion.HasValue &&
            string.IsNullOrWhiteSpace(pago.MotivoAnulacion))
        {
            return string.Empty;
        }

        return $$"""
        <section class="section">
          <h2>Anulaciones y devoluciones</h2>
          <table>
            {{Row("Fecha anulación", FormatDate(pago.FechaAnulacion))}}
            {{Row("Anulado por", pago.AnuladoPor?.Email)}}
            {{Row("Motivo anulación", pago.MotivoAnulacion)}}
            {{Row("Referencia anulación", pago.ReferenciaAnulacion)}}
            {{Row("Última devolución", FormatDate(pago.FechaUltimaDevolucion))}}
          </table>
        </section>
""";
    }

    private static string Row(string label, string? value)
    {
        return $"<tr><td>{Html(label)}</td><td>{Html(string.IsNullOrWhiteSpace(value) ? "No registrado" : value)}</td></tr>";
    }

    private static string Html(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    private static string FormatDate(DateTime? value)
    {
        return value.HasValue
            ? value.Value.ToString("dd-MM-yyyy HH:mm", ChileCulture)
            : "No registrado";
    }

    private static string FormatMoney(decimal value, string moneda)
    {
        return $"{moneda} {value.ToString("N0", ChileCulture)}";
    }

    private static string BuildComprobanteFileName(Pago pago)
    {
        var negocio = SanitizeFileName(string.IsNullOrWhiteSpace(pago.Negocio.Slug)
            ? pago.Negocio.Nombre
            : pago.Negocio.Slug);
        var codigo = SanitizeFileName(pago.Cita.Codigo);

        return $"comprobante-{negocio}-{codigo}-pago-{pago.IdPago}.html";
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        var sanitized = new string(value
            .Select(character => invalidChars.Contains(character) ? '-' : character)
            .ToArray());

        sanitized = sanitized.Trim('-', ' ', '.');
        return string.IsNullOrWhiteSpace(sanitized) ? "pago" : sanitized;
    }
}
