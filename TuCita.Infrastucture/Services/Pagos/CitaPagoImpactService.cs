using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TuCita.Application.Auditoria;
using TuCita.Application.Common;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Pagos;

public sealed class CitaPagoImpactService(
    ReservaFlowDbContext dbContext,
    IAuditoriaService auditoriaService) : ICitaPagoImpactService
{
    private const string PaymentPendingStateName = "Pendiente de pago";
    private const string ConfirmedStateName = "Confirmada";
    private const string PendingStateName = "Pendiente";
    private const string PaidPaymentStateName = "Pagado";
    private const string PartiallyRefundedPaymentStateName = "Parcialmente devuelto";
    private const string PendingPaymentStateName = "Pendiente";

    public CitaPagoSnapshot Capture(Cita cita)
    {
        return new CitaPagoSnapshot(
            cita.IdEstadoCita,
            cita.IdServicio,
            cita.IdPrestador,
            cita.FechaInicio,
            cita.FechaFin,
            cita.PrecioEstimado);
    }

    public async Task RegistrarActualizacionAsync(
        Cita cita,
        CitaPagoSnapshot snapshot,
        string? userId,
        string motivo,
        CancellationToken cancellationToken)
    {
        await EnsurePaymentGraphLoadedAsync(cita, cancellationToken);

        if (!TienePagosRelevantes(cita))
        {
            return;
        }

        var cambioAgenda = snapshot.FechaInicio != cita.FechaInicio ||
            snapshot.FechaFin != cita.FechaFin ||
            snapshot.IdPrestador != cita.IdPrestador;
        var cambioComercial = snapshot.IdServicio != cita.IdServicio ||
            snapshot.PrecioEstimado != cita.PrecioEstimado;

        if (cambioAgenda)
        {
            AddHistorialPagos(
                cita,
                "CitaReagendada",
                userId,
                motivo,
                "El pago se mantiene asociado a la cita reagendada.",
                snapshot);
            await RegistrarAuditoriaAsync(
                cita,
                snapshot,
                userId,
                "CitaReagendada",
                "Cita con pagos asociados reagendada.",
                motivo,
                cancellationToken);
        }

        if (cambioComercial)
        {
            AddHistorialPagos(
                cita,
                "AjusteCitaConPago",
                userId,
                motivo,
                "La cita con pagos asociados cambio de servicio o monto. Revisar saldo neto y diferencias si corresponde.",
                snapshot);
            await RegistrarAuditoriaAsync(
                cita,
                snapshot,
                userId,
                "AjusteCitaConPago",
                "Cita con pagos asociados cambio de servicio o monto.",
                motivo,
                cancellationToken);

            await RecalcularEstadoPorSaldoAsync(cita, snapshot.IdEstadoCita, userId, cancellationToken);
        }
    }

    public async Task RegistrarReagendamientoAsync(
        Cita cita,
        CitaPagoSnapshot snapshot,
        string? userId,
        string motivo,
        CancellationToken cancellationToken)
    {
        await EnsurePaymentGraphLoadedAsync(cita, cancellationToken);

        if (!TienePagosRelevantes(cita))
        {
            return;
        }

        AddHistorialPagos(
            cita,
            "CitaReagendada",
            userId,
            motivo,
            "El pago se mantiene asociado a la cita reagendada.",
            snapshot);
        await RegistrarAuditoriaAsync(
            cita,
            snapshot,
            userId,
            "CitaReagendada",
            "Cita con pagos asociados reagendada.",
            motivo,
            cancellationToken);
    }

    public async Task RegistrarCancelacionAsync(
        Cita cita,
        CitaPagoSnapshot snapshot,
        string? userId,
        string motivo,
        CancellationToken cancellationToken)
    {
        await EnsurePaymentGraphLoadedAsync(cita, cancellationToken);

        if (!TienePagosRelevantes(cita))
        {
            return;
        }

        var totalPagadoNeto = GetTotalPagadoNeto(cita);
        var tipoEvento = totalPagadoNeto > 0m
            ? "RevisionDevolucionPendiente"
            : "CitaCanceladaPagoPendiente";
        var mensaje = totalPagadoNeto > 0m
            ? "La cita fue cancelada con saldo pagado. Registrar devolución manual si corresponde."
            : "La cita fue cancelada con pagos pendientes asociados. Revisar si deben anularse o expirar.";

        AddHistorialPagos(cita, tipoEvento, userId, motivo, mensaje, snapshot);
        await RegistrarAuditoriaAsync(
            cita,
            snapshot,
            userId,
            tipoEvento,
            mensaje,
            motivo,
            cancellationToken);
    }

    private async Task EnsurePaymentGraphLoadedAsync(Cita cita, CancellationToken cancellationToken)
    {
        var citaEntry = dbContext.Entry(cita);

        if (!citaEntry.Collection(item => item.Pagos).IsLoaded)
        {
            await citaEntry.Collection(item => item.Pagos)
                .Query()
                .Include(pago => pago.EstadoPago)
                .LoadAsync(cancellationToken);
        }

        if (!citaEntry.Reference(item => item.Servicio).IsLoaded)
        {
            await citaEntry.Reference(item => item.Servicio).LoadAsync(cancellationToken);
        }

        if (!citaEntry.Reference(item => item.EstadoCita).IsLoaded)
        {
            await citaEntry.Reference(item => item.EstadoCita).LoadAsync(cancellationToken);
        }

        foreach (var pago in cita.Pagos)
        {
            var pagoEntry = dbContext.Entry(pago);
            if (!pagoEntry.Reference(item => item.EstadoPago).IsLoaded)
            {
                await pagoEntry.Reference(item => item.EstadoPago).LoadAsync(cancellationToken);
            }
        }
    }

    private async Task RecalcularEstadoPorSaldoAsync(
        Cita cita,
        int estadoAnterior,
        string? userId,
        CancellationToken cancellationToken)
    {
        if (cita.EstadoCita.EsEstadoFinal ||
            !cita.Servicio.RequierePagoAnticipado ||
            cita.PrecioEstimado <= 0)
        {
            return;
        }

        var totalPagadoNeto = GetTotalPagadoNeto(cita);
        var estadoDestinoNombre = totalPagadoNeto >= cita.PrecioEstimado
            ? await GetEstadoCitaPostPagoAsync(cita.IdNegocio, cancellationToken)
            : PaymentPendingStateName;

        var estadoDestino = await dbContext.EstadosCita
            .FirstOrDefaultAsync(estado => estado.Nombre == estadoDestinoNombre && estado.Activo, cancellationToken);

        if (estadoDestino is null || cita.IdEstadoCita == estadoDestino.IdEstadoCita)
        {
            return;
        }

        cita.IdEstadoCita = estadoDestino.IdEstadoCita;
        cita.FechaActualizacion = DateTime.Now;
        cita.Historial.Add(new CitaHistorial
        {
            IdNegocio = cita.IdNegocio,
            IdEstadoAnterior = estadoAnterior,
            IdEstadoNuevo = estadoDestino.IdEstadoCita,
            UserId = userId,
            Observacion = "Estado actualizado por ajuste de pagos asociados a la cita."
        });
    }

    private async Task<string> GetEstadoCitaPostPagoAsync(int idNegocio, CancellationToken cancellationToken)
    {
        var requiereConfirmacionManual = await dbContext.ReglasReserva
            .AsNoTracking()
            .Where(regla => regla.IdNegocio == idNegocio)
            .Select(regla => regla.RequiereConfirmacionManual)
            .FirstOrDefaultAsync(cancellationToken);

        return requiereConfirmacionManual ? PendingStateName : ConfirmedStateName;
    }

    private void AddHistorialPagos(
        Cita cita,
        string tipoEvento,
        string? userId,
        string motivo,
        string mensaje,
        CitaPagoSnapshot snapshot)
    {
        var datosJson = BuildDatosJson(cita, snapshot, mensaje);

        foreach (var pago in cita.Pagos.Where(PagoEsRelevante))
        {
            dbContext.PagoHistoriales.Add(new PagoHistorial
            {
                IdPago = pago.IdPago,
                IdNegocio = pago.IdNegocio,
                IdCita = pago.IdCita,
                TipoEvento = tipoEvento,
                EstadoAnterior = pago.EstadoPago.Nombre,
                EstadoNuevo = pago.EstadoPago.Nombre,
                Monto = PagoAportaSaldo(pago) ? GetMontoNeto(pago) : pago.Monto,
                Motivo = NormalizeOptional($"{mensaje} {motivo}", 500),
                Referencia = cita.Codigo,
                UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
                DatosJson = datosJson,
                FechaCreacion = DateTime.Now
            });
        }
    }

    private static string BuildDatosJson(Cita cita, CitaPagoSnapshot snapshot, string mensaje)
    {
        return JsonSerializer.Serialize(new
        {
            mensaje,
            cita = new
            {
                cita.IdCita,
                cita.Codigo,
                cita.IdNegocio,
                estadoActual = cita.IdEstadoCita,
                servicioAnterior = snapshot.IdServicio,
                servicioActual = cita.IdServicio,
                prestadorAnterior = snapshot.IdPrestador,
                prestadorActual = cita.IdPrestador,
                fechaInicioAnterior = snapshot.FechaInicio,
                fechaInicioActual = cita.FechaInicio,
                fechaFinAnterior = snapshot.FechaFin,
                fechaFinActual = cita.FechaFin,
                precioAnterior = snapshot.PrecioEstimado,
                precioActual = cita.PrecioEstimado
            },
            pagos = new
            {
                totalPagadoNeto = GetTotalPagadoNeto(cita),
                saldoPendiente = Math.Max(cita.PrecioEstimado - GetTotalPagadoNeto(cita), 0m),
                sobrePago = Math.Max(GetTotalPagadoNeto(cita) - cita.PrecioEstimado, 0m)
            }
        });
    }

    private Task RegistrarAuditoriaAsync(
        Cita cita,
        CitaPagoSnapshot snapshot,
        string? userId,
        string accion,
        string descripcion,
        string motivo,
        CancellationToken cancellationToken)
    {
        return auditoriaService.RegistrarAsync(
            new CurrentUserContext(userId ?? string.Empty, []),
            new AuditoriaRegistro(
                cita.IdNegocio,
                "Pagos",
                accion,
                nameof(Cita),
                cita.IdCita.ToString(),
                $"{descripcion} Cita: {cita.Codigo}.",
                BuildAuditSnapshot(cita, snapshot, isPrevious: true),
                BuildAuditSnapshot(cita, snapshot, isPrevious: false),
                new
                {
                    motivo,
                    totalPagadoNeto = GetTotalPagadoNeto(cita),
                    saldoPendiente = Math.Max(cita.PrecioEstimado - GetTotalPagadoNeto(cita), 0m),
                    pagosAfectados = cita.Pagos.Count(PagoEsRelevante)
                }),
            cancellationToken);
    }

    private static object BuildAuditSnapshot(Cita cita, CitaPagoSnapshot snapshot, bool isPrevious)
    {
        return new
        {
            cita.IdCita,
            cita.IdNegocio,
            cita.Codigo,
            IdEstadoCita = isPrevious ? snapshot.IdEstadoCita : cita.IdEstadoCita,
            IdServicio = isPrevious ? snapshot.IdServicio : cita.IdServicio,
            IdPrestador = isPrevious ? snapshot.IdPrestador : cita.IdPrestador,
            FechaInicio = isPrevious ? snapshot.FechaInicio : cita.FechaInicio,
            FechaFin = isPrevious ? snapshot.FechaFin : cita.FechaFin,
            PrecioEstimado = isPrevious ? snapshot.PrecioEstimado : cita.PrecioEstimado,
            Pagos = cita.Pagos
                .Where(PagoEsRelevante)
                .Select(pago => new
                {
                    pago.IdPago,
                    pago.IdEstadoPago,
                    EstadoPago = pago.EstadoPago.Nombre,
                    pago.Monto,
                    pago.MontoDevuelto,
                    MontoNeto = GetMontoNeto(pago),
                    pago.Proveedor,
                    pago.EsManual
                })
                .ToArray()
        };
    }

    private static bool TienePagosRelevantes(Cita cita)
    {
        return cita.Pagos.Any(PagoEsRelevante);
    }

    private static bool PagoEsRelevante(Pago pago)
    {
        return PagoAportaSaldo(pago) ||
            pago.EstadoPago.Nombre.Equals(PendingPaymentStateName, StringComparison.OrdinalIgnoreCase);
    }

    private static decimal GetTotalPagadoNeto(Cita cita)
    {
        return cita.Pagos
            .Where(PagoAportaSaldo)
            .Sum(GetMontoNeto);
    }

    private static decimal GetMontoNeto(Pago pago)
    {
        return Math.Max(pago.Monto - pago.MontoDevuelto, 0m);
    }

    private static bool PagoAportaSaldo(Pago pago)
    {
        return pago.EstadoPago.Nombre.Equals(PaidPaymentStateName, StringComparison.OrdinalIgnoreCase) ||
            pago.EstadoPago.Nombre.Equals(PartiallyRefundedPaymentStateName, StringComparison.OrdinalIgnoreCase);
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
}
