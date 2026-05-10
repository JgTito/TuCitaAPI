using Microsoft.EntityFrameworkCore;
using TuCita.Application.Common;
using TuCita.Application.Dashboard;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Dashboard;

public sealed class DashboardNegocioService(ReservaFlowDbContext dbContext) : IDashboardNegocioService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";
    private const string RecepcionistaRoleName = "Recepcionista";
    private const string PendingStateName = "Pendiente";
    private const string ConfirmedStateName = "Confirmada";
    private const string CancelledStateName = "Cancelada";
    private const string AttendedStateName = "Atendida";
    private const string NoShowStateName = "No asistio";
    private const string NoShowStateNameWithAccent = "No asistió";
    private const string PaymentPendingStateName = "Pendiente";
    private const string PaymentPaidStateName = "Pagado";
    private const string PaymentPartiallyRefundedStateName = "Parcialmente devuelto";
    private const string PaymentRefundedStateName = "Devuelto";
    private const string PaymentRejectedStateName = "Rechazado";
    private const string PaymentCancelledStateName = "Anulado";
    private const string PaymentErrorStateName = "Error";
    private const string ReviewApprovedStateName = "Aprobada";
    private const string ReviewPendingStateName = "Pendiente";
    private const int MaxRangeDays = 366;

    public async Task<ServiceResult<DashboardNegocioDto>> GetAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        DashboardNegocioQuery query,
        CancellationToken cancellationToken)
    {
        var negocio = await dbContext.Negocios
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdNegocio == idNegocio, cancellationToken);

        if (negocio is null)
        {
            return ServiceResult<DashboardNegocioDto>.NotFound("El negocio no existe.");
        }

        if (!await CanAccessDashboardAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<DashboardNegocioDto>.Forbidden("No tienes acceso para ver el dashboard de este negocio.");
        }

        var range = NormalizeRange(query);
        if (!range.IsValid)
        {
            return ServiceResult<DashboardNegocioDto>.Validation([
                new ValidationError(string.Empty, range.Error!)
            ]);
        }

        var todayStart = DateTime.Today;
        var todayEnd = todayStart.AddDays(1);
        var now = DateTime.Now;
        var diasProximasCitas = GetDiasProximasCitas(query);
        var limiteProximasCitas = GetLimiteProximasCitas(query);
        var proximasHasta = now.AddDays(diasProximasCitas);

        var citasRango = await dbContext.Citas
            .AsNoTracking()
            .Where(cita =>
                cita.IdNegocio == idNegocio &&
                cita.FechaInicio >= range.FechaDesde &&
                cita.FechaInicio < range.FechaHastaExclusive)
            .Select(cita => new DashboardCitaData(
                cita.IdCita,
                cita.Codigo,
                cita.FechaInicio,
                cita.FechaFin,
                cita.IdCliente,
                cita.Cliente.Nombre,
                cita.IdServicio,
                cita.Servicio.Nombre,
                cita.IdPrestador,
                cita.Prestador == null ? null : cita.Prestador.Nombre,
                cita.IdEstadoCita,
                cita.EstadoCita.Nombre,
                cita.EstadoCita.EsEstadoFinal,
                cita.PrecioEstimado))
            .ToArrayAsync(cancellationToken);

        var citasHoy = await dbContext.Citas.CountAsync(
            cita =>
                cita.IdNegocio == idNegocio &&
                cita.FechaInicio >= todayStart &&
                cita.FechaInicio < todayEnd,
            cancellationToken);

        var proximasCitasCount = await dbContext.Citas.CountAsync(
            cita =>
                cita.IdNegocio == idNegocio &&
                cita.FechaInicio >= now &&
                cita.FechaInicio < proximasHasta &&
                !cita.EstadoCita.EsEstadoFinal,
            cancellationToken);

        var proximasCitas = await dbContext.Citas
            .AsNoTracking()
            .Where(cita =>
                cita.IdNegocio == idNegocio &&
                cita.FechaInicio >= now &&
                cita.FechaInicio < proximasHasta &&
                !cita.EstadoCita.EsEstadoFinal)
            .OrderBy(cita => cita.FechaInicio)
            .Take(limiteProximasCitas)
            .Select(cita => new DashboardProximaCitaDto(
                cita.IdCita,
                cita.Codigo,
                cita.FechaInicio,
                cita.FechaFin,
                cita.IdCliente,
                cita.Cliente.Nombre,
                cita.IdServicio,
                cita.Servicio.Nombre,
                cita.IdPrestador,
                cita.Prestador == null ? null : cita.Prestador.Nombre,
                cita.IdEstadoCita,
                cita.EstadoCita.Nombre,
                cita.PrecioEstimado))
            .ToArrayAsync(cancellationToken);

        var resenasRango = await dbContext.ResenasNegocio
            .AsNoTracking()
            .Where(resena =>
                resena.IdNegocio == idNegocio &&
                resena.FechaCreacion >= range.FechaDesde &&
                resena.FechaCreacion < range.FechaHastaExclusive &&
                resena.Activo)
            .Select(resena => new DashboardResenaData(
                resena.IdResenaNegocio,
                resena.IdCita,
                resena.Cita.Codigo,
                resena.ClienteNombreSnapshot,
                resena.ServicioNombreSnapshot,
                resena.PrestadorNombreSnapshot,
                resena.Puntuacion,
                resena.Estado,
                resena.EsVisiblePublicamente,
                resena.FechaCreacion))
            .ToArrayAsync(cancellationToken);

        var resenasPublicas = resenasRango
            .Where(resena => IsApprovedPublicReview(resena))
            .ToArray();

        var resumen = new DashboardResumenDto(
            citasHoy,
            proximasCitasCount,
            citasRango.Count(cita => cita.Estado.Equals(PendingStateName, StringComparison.OrdinalIgnoreCase)),
            citasRango.Count(IsCancelled),
            citasRango.Count(IsNoShow),
            citasRango.Length,
            citasRango.Where(CountsAsEstimatedIncome).Sum(cita => cita.PrecioEstimado),
            resenasPublicas.Length == 0 ? 0m : Math.Round((decimal)resenasPublicas.Average(resena => resena.Puntuacion), 2),
            resenasRango.Length,
            resenasRango.Count(resena => resena.Estado.Equals(ReviewPendingStateName, StringComparison.OrdinalIgnoreCase)));

        var dashboard = new DashboardNegocioDto(
            negocio.IdNegocio,
            negocio.Nombre,
            range.FechaDesde,
            range.FechaHastaInclusive,
            resumen,
            BuildEstadoSerie(citasRango),
            BuildDailySerie(citasRango, range.FechaDesde, range.FechaHastaInclusive),
            await BuildProviderOccupancyAsync(idNegocio, citasRango, range.FechaDesde, range.FechaHastaInclusive, cancellationToken),
            proximasCitas,
            BuildReviewDistribution(resenasPublicas),
            BuildRecentReviews(resenasRango));

        return ServiceResult<DashboardNegocioDto>.Success(dashboard);
    }

    public async Task<ServiceResult<DashboardPagosDto>> GetPagosAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        DashboardPagosQuery query,
        CancellationToken cancellationToken)
    {
        var negocio = await dbContext.Negocios
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdNegocio == idNegocio, cancellationToken);

        if (negocio is null)
        {
            return ServiceResult<DashboardPagosDto>.NotFound("El negocio no existe.");
        }

        if (!await CanAccessPaymentDashboardAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<DashboardPagosDto>.Forbidden("No tienes acceso para ver el dashboard financiero de este negocio.");
        }

        var range = NormalizeRange(query);
        if (!range.IsValid)
        {
            return ServiceResult<DashboardPagosDto>.Validation([
                new ValidationError(string.Empty, range.Error!)
            ]);
        }

        var pagosRango = await dbContext.Pagos
            .AsNoTracking()
            .Where(pago =>
                pago.IdNegocio == idNegocio &&
                (pago.FechaPago ?? pago.FechaActualizacion ?? pago.FechaCreacion) >= range.FechaDesde &&
                (pago.FechaPago ?? pago.FechaActualizacion ?? pago.FechaCreacion) < range.FechaHastaExclusive)
            .Select(pago => new DashboardPagoData(
                pago.IdPago,
                pago.IdCita,
                pago.Cita.Codigo,
                pago.Cita.Cliente.Nombre,
                pago.Cita.Servicio.Nombre,
                pago.Proveedor,
                pago.CommerceOrder,
                pago.Monto,
                pago.MontoDevuelto,
                pago.Moneda,
                pago.IdEstadoPago,
                pago.EstadoPago.Nombre,
                pago.EstadoPago.EsEstadoFinal,
                pago.FechaCreacion,
                pago.FechaPago,
                pago.FechaActualizacion,
                pago.FechaPago ?? pago.FechaActualizacion ?? pago.FechaCreacion))
            .ToArrayAsync(cancellationToken);

        var citasEstimadas = await dbContext.Citas
            .AsNoTracking()
            .Where(cita =>
                cita.IdNegocio == idNegocio &&
                cita.FechaInicio >= range.FechaDesde &&
                cita.FechaInicio < range.FechaHastaExclusive)
            .Select(cita => new DashboardCitaData(
                cita.IdCita,
                cita.Codigo,
                cita.FechaInicio,
                cita.FechaFin,
                cita.IdCliente,
                cita.Cliente.Nombre,
                cita.IdServicio,
                cita.Servicio.Nombre,
                cita.IdPrestador,
                cita.Prestador == null ? null : cita.Prestador.Nombre,
                cita.IdEstadoCita,
                cita.EstadoCita.Nombre,
                cita.EstadoCita.EsEstadoFinal,
                cita.PrecioEstimado))
            .ToArrayAsync(cancellationToken);

        var ingresosEstimadosCitas = citasEstimadas
            .Where(CountsAsEstimatedIncome)
            .Sum(cita => cita.PrecioEstimado);
        var limiteUltimosPagos = query.LimiteUltimosPagos ?? DashboardPagosQuery.DefaultLimiteUltimosPagos;

        var dashboard = new DashboardPagosDto(
            negocio.IdNegocio,
            negocio.Nombre,
            range.FechaDesde,
            range.FechaHastaInclusive,
            BuildPaymentSummary(pagosRango, ingresosEstimadosCitas),
            BuildPaymentStateSerie(pagosRango),
            BuildPaymentDailySerie(pagosRango, range.FechaDesde, range.FechaHastaInclusive),
            BuildLatestPayments(pagosRango, limiteUltimosPagos));

        return ServiceResult<DashboardPagosDto>.Success(dashboard);
    }

    private static int GetDiasProximasCitas(DashboardNegocioQuery query)
    {
        return query.DiasProximasCitas ??
            query.ProximasDias ??
            DashboardNegocioQuery.DefaultDiasProximasCitas;
    }

    private static int GetLimiteProximasCitas(DashboardNegocioQuery query)
    {
        return query.LimiteProximasCitas ??
            query.CantidadProximas ??
            DashboardNegocioQuery.DefaultLimiteProximasCitas;
    }

    private async Task<bool> CanAccessDashboardAsync(
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

    private async Task<bool> CanAccessPaymentDashboardAsync(
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
                    item.RolNegocio.Nombre == AdminRoleName),
            cancellationToken);
    }

    private static IReadOnlyCollection<DashboardEstadoCitaSerieDto> BuildEstadoSerie(
        IReadOnlyCollection<DashboardCitaData> citas)
    {
        return citas
            .GroupBy(cita => new { cita.IdEstadoCita, cita.Estado, cita.EsEstadoFinal })
            .OrderBy(group => group.Key.Estado)
            .Select(group => new DashboardEstadoCitaSerieDto(
                group.Key.IdEstadoCita,
                group.Key.Estado,
                group.Key.EsEstadoFinal,
                group.Count(),
                group.Where(CountsAsEstimatedIncome).Sum(cita => cita.PrecioEstimado)))
            .ToArray();
    }

    private static IReadOnlyCollection<DashboardCitasPorDiaDto> BuildDailySerie(
        IReadOnlyCollection<DashboardCitaData> citas,
        DateTime fechaDesde,
        DateTime fechaHasta)
    {
        var citasByDate = citas
            .GroupBy(cita => cita.FechaInicio.Date)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var totalDays = (fechaHasta.Date - fechaDesde.Date).Days + 1;

        return Enumerable.Range(0, totalDays)
            .Select(index =>
            {
                var date = fechaDesde.Date.AddDays(index);
                var dayCitas = citasByDate.TryGetValue(date, out var values) ? values : [];

                return new DashboardCitasPorDiaDto(
                    date,
                    dayCitas.Length,
                    dayCitas.Count(cita => cita.Estado.Equals(PendingStateName, StringComparison.OrdinalIgnoreCase)),
                    dayCitas.Count(cita => cita.Estado.Equals(ConfirmedStateName, StringComparison.OrdinalIgnoreCase)),
                    dayCitas.Count(IsCancelled),
                    dayCitas.Count(cita => cita.Estado.Equals(AttendedStateName, StringComparison.OrdinalIgnoreCase)),
                    dayCitas.Count(IsNoShow),
                    dayCitas.Where(CountsAsEstimatedIncome).Sum(cita => cita.PrecioEstimado));
            })
            .ToArray();
    }

    private static DashboardPagosResumenDto BuildPaymentSummary(
        IReadOnlyCollection<DashboardPagoData> pagos,
        decimal ingresosEstimadosCitas)
    {
        var pagosPagados = pagos.Where(IsPaidPayment).ToArray();
        var montoPagado = pagosPagados.Sum(pago => pago.MontoNeto);
        var totalPagos = pagos.Count;
        var pagosConfirmados = pagosPagados.Length;
        var ticketPromedio = pagosConfirmados > 0
            ? Math.Round(montoPagado / pagosConfirmados, 2)
            : 0m;
        var tasaConfirmados = totalPagos > 0
            ? Math.Round(pagosConfirmados * 100m / totalPagos, 2)
            : 0m;

        return new DashboardPagosResumenDto(
            totalPagos,
            pagosConfirmados,
            pagos.Count(IsPendingPayment),
            pagos.Count(IsRejectedPayment),
            pagos.Count(IsCancelledPayment),
            pagos.Count(IsPartiallyRefundedPayment),
            pagos.Count(IsRefundedPayment),
            pagos.Count(IsErrorPayment),
            pagos.Sum(pago => pago.Monto),
            montoPagado,
            pagos.Where(IsPendingPayment).Sum(pago => pago.Monto),
            pagos.Where(IsRejectedPayment).Sum(pago => pago.Monto),
            pagos.Where(IsCancelledPayment).Sum(pago => pago.Monto),
            pagos.Sum(pago => pago.MontoDevuelto),
            pagos.Where(IsErrorPayment).Sum(pago => pago.Monto),
            ticketPromedio,
            tasaConfirmados,
            ingresosEstimadosCitas,
            ingresosEstimadosCitas - montoPagado);
    }

    private static IReadOnlyCollection<DashboardPagoEstadoDto> BuildPaymentStateSerie(
        IReadOnlyCollection<DashboardPagoData> pagos)
    {
        return pagos
            .GroupBy(pago => new { pago.IdEstadoPago, pago.EstadoPago, pago.EsEstadoFinal })
            .OrderBy(group => group.Key.EstadoPago)
            .Select(group => new DashboardPagoEstadoDto(
                group.Key.IdEstadoPago,
                group.Key.EstadoPago,
                group.Key.EsEstadoFinal,
                group.Count(),
                group.Sum(pago => pago.Monto),
                group.Sum(pago => pago.MontoDevuelto),
                group.Sum(pago => pago.MontoNeto)))
            .ToArray();
    }

    private static IReadOnlyCollection<DashboardPagosPorDiaDto> BuildPaymentDailySerie(
        IReadOnlyCollection<DashboardPagoData> pagos,
        DateTime fechaDesde,
        DateTime fechaHasta)
    {
        var pagosByDate = pagos
            .GroupBy(pago => pago.FechaReferencia.Date)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var totalDays = (fechaHasta.Date - fechaDesde.Date).Days + 1;

        return Enumerable.Range(0, totalDays)
            .Select(index =>
            {
                var date = fechaDesde.Date.AddDays(index);
                var dayPayments = pagosByDate.TryGetValue(date, out var values) ? values : [];

                return new DashboardPagosPorDiaDto(
                    date,
                    dayPayments.Length,
                    dayPayments.Count(IsPaidPayment),
                    dayPayments.Count(IsPendingPayment),
                    dayPayments.Count(IsRejectedPayment),
                    dayPayments.Count(IsCancelledPayment),
                    dayPayments.Count(IsPartiallyRefundedPayment),
                    dayPayments.Count(IsRefundedPayment),
                    dayPayments.Count(IsErrorPayment),
                    dayPayments.Where(IsPaidPayment).Sum(pago => pago.MontoNeto),
                    dayPayments.Sum(pago => pago.MontoDevuelto),
                    dayPayments.Where(IsPendingPayment).Sum(pago => pago.Monto),
                    dayPayments.Where(IsRejectedPayment).Sum(pago => pago.Monto));
            })
            .ToArray();
    }

    private static IReadOnlyCollection<DashboardUltimoPagoDto> BuildLatestPayments(
        IReadOnlyCollection<DashboardPagoData> pagos,
        int limit)
    {
        return pagos
            .OrderByDescending(pago => pago.FechaReferencia)
            .ThenByDescending(pago => pago.IdPago)
            .Take(limit)
            .Select(pago => new DashboardUltimoPagoDto(
                pago.IdPago,
                pago.IdCita,
                pago.CodigoCita,
                pago.Cliente,
                pago.Servicio,
                pago.Proveedor,
                pago.CommerceOrder,
                pago.Monto,
                pago.MontoDevuelto,
                pago.MontoNeto,
                pago.Moneda,
                pago.IdEstadoPago,
                pago.EstadoPago,
                pago.FechaCreacion,
                pago.FechaPago,
                pago.FechaActualizacion))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<DashboardPrestadorOcupacionDto>> BuildProviderOccupancyAsync(
        int idNegocio,
        IReadOnlyCollection<DashboardCitaData> citas,
        DateTime fechaDesde,
        DateTime fechaHasta,
        CancellationToken cancellationToken)
    {
        var prestadores = await dbContext.Prestadores
            .AsNoTracking()
            .Include(prestador => prestador.TipoPrestador)
            .Where(prestador => prestador.IdNegocio == idNegocio)
            .OrderBy(prestador => prestador.Nombre)
            .Select(prestador => new
            {
                prestador.IdPrestador,
                prestador.Nombre,
                TipoPrestador = prestador.TipoPrestador.Nombre,
                prestador.Activo
            })
            .ToArrayAsync(cancellationToken);

        var horarios = await dbContext.HorariosPrestador
            .AsNoTracking()
            .Where(horario => horario.IdNegocio == idNegocio && horario.Activo)
            .Select(horario => new HorarioPrestadorData(
                horario.IdPrestador,
                horario.DiaSemana,
                horario.HoraInicio,
                horario.HoraFin))
            .ToArrayAsync(cancellationToken);

        var citasByPrestador = citas
            .Where(cita => cita.IdPrestador.HasValue)
            .GroupBy(cita => cita.IdPrestador!.Value)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var result = new List<DashboardPrestadorOcupacionDto>();

        foreach (var prestador in prestadores)
        {
            var providerCitas = citasByPrestador.TryGetValue(prestador.IdPrestador, out var values) ? values : [];
            if (!prestador.Activo && providerCitas.Length == 0)
            {
                continue;
            }

            var minutosReservados = providerCitas
                .Where(cita => !IsCancelled(cita))
                .Sum(GetDurationMinutes);
            var minutosDisponibles = CalculateAvailableMinutes(
                prestador.IdPrestador,
                horarios,
                fechaDesde.Date,
                fechaHasta.Date);

            var porcentaje = minutosDisponibles > 0
                ? Math.Round((decimal)minutosReservados * 100m / minutosDisponibles, 2)
                : 0m;

            result.Add(new DashboardPrestadorOcupacionDto(
                prestador.IdPrestador,
                prestador.Nombre,
                prestador.TipoPrestador,
                providerCitas.Length,
                minutosReservados,
                minutosDisponibles,
                porcentaje,
                providerCitas.Where(CountsAsEstimatedIncome).Sum(cita => cita.PrecioEstimado)));
        }

        return result
            .OrderByDescending(item => item.PorcentajeOcupacion)
            .ThenBy(item => item.Prestador)
            .ToArray();
    }

    private static int CalculateAvailableMinutes(
        int idPrestador,
        IEnumerable<HorarioPrestadorData> horarios,
        DateTime fechaDesde,
        DateTime fechaHasta)
    {
        var providerSchedules = horarios
            .Where(horario => horario.IdPrestador == idPrestador)
            .ToArray();

        var total = 0;
        for (var date = fechaDesde.Date; date <= fechaHasta.Date; date = date.AddDays(1))
        {
            var diaSemana = GetDiaSemana(date.DayOfWeek);
            total += providerSchedules
                .Where(horario => horario.DiaSemana == diaSemana)
                .Sum(horario => (int)(horario.HoraFin - horario.HoraInicio).TotalMinutes);
        }

        return total;
    }

    private static DashboardRange NormalizeRange(DashboardNegocioQuery query)
    {
        return NormalizeRange(query.FechaDesde, query.FechaHasta);
    }

    private static DashboardRange NormalizeRange(DashboardPagosQuery query)
    {
        return NormalizeRange(query.FechaDesde, query.FechaHasta);
    }

    private static DashboardRange NormalizeRange(DateTime? fechaDesdeValue, DateTime? fechaHastaValue)
    {
        var today = DateTime.Today;
        var fechaDesde = (fechaDesdeValue ?? new DateTime(today.Year, today.Month, 1)).Date;
        var fechaHastaInclusive = (fechaHastaValue ?? today).Date;

        if (fechaHastaInclusive < fechaDesde)
        {
            return DashboardRange.Invalid("La fecha hasta debe ser mayor o igual a la fecha desde.");
        }

        if ((fechaHastaInclusive - fechaDesde).Days + 1 > MaxRangeDays)
        {
            return DashboardRange.Invalid($"El rango del dashboard no puede superar {MaxRangeDays} días.");
        }

        return DashboardRange.Valid(fechaDesde, fechaHastaInclusive);
    }

    private static byte GetDiaSemana(DayOfWeek dayOfWeek)
    {
        return dayOfWeek == DayOfWeek.Sunday ? (byte)7 : (byte)dayOfWeek;
    }

    private static int GetDurationMinutes(DashboardCitaData cita)
    {
        return Math.Max(0, (int)Math.Round((cita.FechaFin - cita.FechaInicio).TotalMinutes));
    }

    private static bool CountsAsEstimatedIncome(DashboardCitaData cita)
    {
        return !IsCancelled(cita) && !IsNoShow(cita);
    }

    private static bool IsCancelled(DashboardCitaData cita)
    {
        return cita.Estado.Equals(CancelledStateName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNoShow(DashboardCitaData cita)
    {
        return cita.Estado.Equals(NoShowStateName, StringComparison.OrdinalIgnoreCase) ||
            cita.Estado.Equals(NoShowStateNameWithAccent, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPaidPayment(DashboardPagoData pago)
    {
        return pago.EstadoPago.Equals(PaymentPaidStateName, StringComparison.OrdinalIgnoreCase) ||
            pago.EstadoPago.Equals(PaymentPartiallyRefundedStateName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPendingPayment(DashboardPagoData pago)
    {
        return pago.EstadoPago.Equals(PaymentPendingStateName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRejectedPayment(DashboardPagoData pago)
    {
        return pago.EstadoPago.Equals(PaymentRejectedStateName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCancelledPayment(DashboardPagoData pago)
    {
        return pago.EstadoPago.Equals(PaymentCancelledStateName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPartiallyRefundedPayment(DashboardPagoData pago)
    {
        return pago.EstadoPago.Equals(PaymentPartiallyRefundedStateName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRefundedPayment(DashboardPagoData pago)
    {
        return pago.EstadoPago.Equals(PaymentRefundedStateName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsErrorPayment(DashboardPagoData pago)
    {
        return pago.EstadoPago.Equals(PaymentErrorStateName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsApprovedPublicReview(DashboardResenaData resena)
    {
        return resena.Estado.Equals(ReviewApprovedStateName, StringComparison.OrdinalIgnoreCase) &&
            resena.EsVisiblePublicamente;
    }

    private static IReadOnlyCollection<DashboardResenaPuntuacionDto> BuildReviewDistribution(
        IReadOnlyCollection<DashboardResenaData> resenas)
    {
        return Enumerable.Range(1, 5)
            .Select(puntuacion => new DashboardResenaPuntuacionDto((byte)puntuacion, resenas.Count(resena => resena.Puntuacion == puntuacion)))
            .ToArray();
    }

    private static IReadOnlyCollection<DashboardResenaRecienteDto> BuildRecentReviews(
        IReadOnlyCollection<DashboardResenaData> resenas)
    {
        return resenas
            .OrderByDescending(resena => resena.FechaCreacion)
            .Take(5)
            .Select(resena => new DashboardResenaRecienteDto(
                resena.IdResenaNegocio,
                resena.IdCita,
                resena.CodigoCita,
                resena.Cliente,
                resena.Servicio,
                resena.Prestador,
                resena.Puntuacion,
                resena.Estado,
                resena.EsVisiblePublicamente,
                resena.FechaCreacion))
            .ToArray();
    }

    private sealed record DashboardCitaData(
        int IdCita,
        string Codigo,
        DateTime FechaInicio,
        DateTime FechaFin,
        int IdCliente,
        string Cliente,
        int IdServicio,
        string Servicio,
        int? IdPrestador,
        string? Prestador,
        int IdEstadoCita,
        string Estado,
        bool EsEstadoFinal,
        decimal PrecioEstimado);

    private sealed record DashboardPagoData(
        int IdPago,
        int IdCita,
        string CodigoCita,
        string Cliente,
        string Servicio,
        string Proveedor,
        string CommerceOrder,
        decimal Monto,
        decimal MontoDevuelto,
        string Moneda,
        int IdEstadoPago,
        string EstadoPago,
        bool EsEstadoFinal,
        DateTime FechaCreacion,
        DateTime? FechaPago,
        DateTime? FechaActualizacion,
        DateTime FechaReferencia)
    {
        public decimal MontoNeto =>
            EstadoPago.Equals(PaymentPaidStateName, StringComparison.OrdinalIgnoreCase) ||
            EstadoPago.Equals(PaymentPartiallyRefundedStateName, StringComparison.OrdinalIgnoreCase)
                ? Math.Max(Monto - MontoDevuelto, 0m)
                : 0m;
    }

    private sealed record DashboardResenaData(
        int IdResenaNegocio,
        int IdCita,
        string CodigoCita,
        string Cliente,
        string Servicio,
        string? Prestador,
        byte Puntuacion,
        string Estado,
        bool EsVisiblePublicamente,
        DateTime FechaCreacion);

    private sealed record HorarioPrestadorData(
        int IdPrestador,
        byte DiaSemana,
        TimeOnly HoraInicio,
        TimeOnly HoraFin);

    private sealed record DashboardRange(
        bool IsValid,
        DateTime FechaDesde,
        DateTime FechaHastaInclusive,
        DateTime FechaHastaExclusive,
        string? Error)
    {
        public static DashboardRange Valid(DateTime fechaDesde, DateTime fechaHastaInclusive)
        {
            return new DashboardRange(true, fechaDesde, fechaHastaInclusive, fechaHastaInclusive.AddDays(1), null);
        }

        public static DashboardRange Invalid(string error)
        {
            return new DashboardRange(false, default, default, default, error);
        }
    }
}
