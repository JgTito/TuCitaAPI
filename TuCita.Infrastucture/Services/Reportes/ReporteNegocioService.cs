using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TuCita.Application.Common;
using TuCita.Application.Reportes;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Reportes;

public sealed class ReporteNegocioService(ReservaFlowDbContext dbContext) : IReporteNegocioService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";
    private const int MaxRangeDays = 366;
    private const string CancelledStateName = "CANCELADA";
    private const string AttendedStateName = "ATENDIDA";
    private const string PendingStateName = "PENDIENTE";
    private const string ConfirmedStateName = "CONFIRMADA";
    private const string NoShowStateName = "NO ASISTIO";

    public async Task<ServiceResult<ReporteExcelDto>> ExportExcelAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        ReporteNegocioQuery query,
        CancellationToken cancellationToken)
    {
        var negocio = await dbContext.Negocios
            .AsNoTracking()
            .Where(item => item.IdNegocio == idNegocio)
            .Select(item => new NegocioReporteData(
                item.IdNegocio,
                item.Nombre,
                item.Slug,
                item.Rubro.Nombre))
            .FirstOrDefaultAsync(cancellationToken);

        if (negocio is null)
        {
            return ServiceResult<ReporteExcelDto>.NotFound("El negocio no existe.");
        }

        if (!await CanAccessReportsAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<ReporteExcelDto>.Forbidden("No tienes acceso para exportar reportes de este negocio.");
        }

        var range = NormalizeRange(query);
        if (!range.IsValid)
        {
            return ServiceResult<ReporteExcelDto>.Validation([
                new ValidationError(range.Field ?? string.Empty, range.Error!)
            ]);
        }

        var citas = await dbContext.Citas
            .AsNoTracking()
            .Where(cita =>
                cita.IdNegocio == idNegocio &&
                cita.FechaInicio >= range.FechaDesde &&
                cita.FechaInicio < range.FechaHastaExclusive)
            .Select(cita => new CitaReporteData(
                cita.IdCita,
                cita.Codigo,
                cita.FechaInicio,
                cita.FechaFin,
                cita.IdCliente,
                cita.Cliente.Nombre,
                cita.Cliente.Email,
                cita.IdServicio,
                cita.Servicio.Nombre,
                cita.IdPrestador,
                cita.Prestador == null ? null : cita.Prestador.Nombre,
                cita.IdEstadoCita,
                cita.EstadoCita.Nombre,
                cita.EstadoCita.EsEstadoFinal,
                cita.PrecioEstimado))
            .ToArrayAsync(cancellationToken);

        var prestadores = await dbContext.Prestadores
            .AsNoTracking()
            .Where(prestador => prestador.IdNegocio == idNegocio)
            .Select(prestador => new PrestadorReporteData(
                prestador.IdPrestador,
                prestador.Nombre,
                prestador.TipoPrestador.Nombre,
                prestador.Activo))
            .ToArrayAsync(cancellationToken);

        var workbook = BuildWorkbook(negocio, range, query, citas, prestadores);
        var content = workbook.ToByteArray();
        var fileName = BuildFileName(negocio, range);

        return ServiceResult<ReporteExcelDto>.Success(new ReporteExcelDto(
            fileName,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            content));
    }

    private async Task<bool> CanAccessReportsAsync(
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

    private static SimpleExcelWorkbook BuildWorkbook(
        NegocioReporteData negocio,
        ReportRange range,
        ReporteNegocioQuery query,
        IReadOnlyCollection<CitaReporteData> citas,
        IReadOnlyCollection<PrestadorReporteData> prestadores)
    {
        var workbook = new SimpleExcelWorkbook();
        var metrics = BuildMetrics(citas);

        BuildResumenSheet(workbook, negocio, range, metrics);
        BuildEstadosSheet(workbook, citas);
        BuildIngresosSheet(workbook, range, citas);
        BuildServiciosSheet(workbook, citas, query.Top);
        BuildClientesSheet(workbook, citas, query.Top);
        BuildInasistenciaSheet(workbook, citas);
        BuildPrestadoresSheet(workbook, citas, prestadores);

        if (query.IncluirDetalle)
        {
            BuildDetalleSheet(workbook, citas);
        }

        return workbook;
    }

    private static void BuildResumenSheet(
        SimpleExcelWorkbook workbook,
        NegocioReporteData negocio,
        ReportRange range,
        ReportMetrics metrics)
    {
        var sheet = workbook.AddWorksheet("Resumen");
        sheet.SetColumnWidths(28, 18, 48);
        sheet.FrozenRows = 0;

        AddBandRow(sheet, "Reporte ejecutivo de negocio", 3, ExcelStyles.Title);
        sheet.AddEmptyRow();
        sheet.AddRow(
            SimpleExcelCell.Text("Negocio", ExcelStyles.Section),
            SimpleExcelCell.Text(negocio.Nombre),
            SimpleExcelCell.Text("Rubro: " + negocio.Rubro));
        sheet.AddRow(
            SimpleExcelCell.Text("Período", ExcelStyles.Section),
            SimpleExcelCell.Text($"{range.FechaDesde:yyyy-MM-dd} a {range.FechaHastaInclusive:yyyy-MM-dd}"),
            SimpleExcelCell.Text("Generado: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)));
        sheet.AddEmptyRow();

        sheet.AddRow(
            SimpleExcelCell.Text("Indicador", ExcelStyles.Header),
            SimpleExcelCell.Text("Valor", ExcelStyles.Header),
            SimpleExcelCell.Text("Lectura", ExcelStyles.Header));
        sheet.AddRow(SimpleExcelCell.Text("Total de citas"), SimpleExcelCell.Integer(metrics.TotalCitas), SimpleExcelCell.Text("Volumen total del período."));
        sheet.AddRow(SimpleExcelCell.Text("Citas pendientes"), SimpleExcelCell.Integer(metrics.CitasPendientes), SimpleExcelCell.Text("Reservas que requieren gestión."));
        sheet.AddRow(SimpleExcelCell.Text("Citas confirmadas"), SimpleExcelCell.Integer(metrics.CitasConfirmadas), SimpleExcelCell.Text("Reservas activas ya confirmadas."));
        sheet.AddRow(SimpleExcelCell.Text("Citas canceladas"), SimpleExcelCell.Integer(metrics.CitasCanceladas), SimpleExcelCell.Text("No aportan a ingresos estimados."));
        sheet.AddRow(SimpleExcelCell.Text("Citas atendidas"), SimpleExcelCell.Integer(metrics.CitasAtendidas), SimpleExcelCell.Text("Servicios efectivamente atendidos."));
        sheet.AddRow(SimpleExcelCell.Text("No asistidas"), SimpleExcelCell.Integer(metrics.CitasNoAsistidas), SimpleExcelCell.Text("Base para controlar inasistencia."));
        sheet.AddRow(SimpleExcelCell.Text("Ingresos estimados"), SimpleExcelCell.Currency(metrics.IngresosEstimados), SimpleExcelCell.Text("Excluye canceladas y no asistidas."));
        sheet.AddRow(SimpleExcelCell.Text("Ticket promedio estimado"), SimpleExcelCell.Currency(metrics.TicketPromedio), SimpleExcelCell.Text("Ingreso estimado / citas con ingreso."));
        sheet.AddRow(SimpleExcelCell.Text("Tasa de inasistencia"), SimpleExcelCell.Percent(metrics.TasaInasistencia), SimpleExcelCell.Text("No asistidas / (atendidas + no asistidas)."));
        sheet.AddRow(SimpleExcelCell.Text("Clientes únicos"), SimpleExcelCell.Integer(metrics.ClientesUnicos), SimpleExcelCell.Text("Clientes con al menos una cita."));
        sheet.AddRow(SimpleExcelCell.Text("Servicios reservados"), SimpleExcelCell.Integer(metrics.ServiciosReservados), SimpleExcelCell.Text("Servicios distintos con reservas."));
        sheet.AddRow(SimpleExcelCell.Text("Prestadores con citas"), SimpleExcelCell.Integer(metrics.PrestadoresConCitas), SimpleExcelCell.Text("Prestadores o recursos usados en el período."));
    }

    private static void BuildEstadosSheet(SimpleExcelWorkbook workbook, IReadOnlyCollection<CitaReporteData> citas)
    {
        var sheet = workbook.AddWorksheet("Citas por estado");
        sheet.SetColumnWidths(24, 14, 14, 16, 20);
        AddBandRow(sheet, "Citas por estado", 5, ExcelStyles.Title);
        sheet.AddEmptyRow();
        sheet.AddRow(
            SimpleExcelCell.Text("Estado", ExcelStyles.Header),
            SimpleExcelCell.Text("Final", ExcelStyles.Header),
            SimpleExcelCell.Text("Citas", ExcelStyles.Header),
            SimpleExcelCell.Text("% del total", ExcelStyles.Header),
            SimpleExcelCell.Text("Ingresos estimados", ExcelStyles.Header));

        var total = Math.Max(1, citas.Count);
        var rowCount = 3;
        foreach (var row in citas
            .GroupBy(cita => new { cita.IdEstadoCita, cita.Estado, cita.EsEstadoFinal })
            .OrderBy(group => group.Key.Estado)
            .Select(group => new
            {
                group.Key.Estado,
                group.Key.EsEstadoFinal,
                Citas = group.Count(),
                Ingresos = group.Where(CountsAsEstimatedIncome).Sum(cita => cita.PrecioEstimado)
            }))
        {
            sheet.AddRow(
                SimpleExcelCell.Text(row.Estado),
                SimpleExcelCell.Text(row.EsEstadoFinal ? "Sí" : "No"),
                SimpleExcelCell.Integer(row.Citas),
                SimpleExcelCell.Percent((decimal)row.Citas / total),
                SimpleExcelCell.Currency(row.Ingresos));
            rowCount++;
        }

        sheet.AutoFilterReference = $"A3:E{Math.Max(3, rowCount)}";
    }

    private static void BuildIngresosSheet(
        SimpleExcelWorkbook workbook,
        ReportRange range,
        IReadOnlyCollection<CitaReporteData> citas)
    {
        var sheet = workbook.AddWorksheet("Ingresos por período");
        sheet.SetColumnWidths(16, 14, 20, 18, 18, 18);
        AddBandRow(sheet, "Ingresos diarios estimados", 6, ExcelStyles.Title);
        sheet.AddEmptyRow();
        sheet.AddRow(
            SimpleExcelCell.Text("Fecha", ExcelStyles.Header),
            SimpleExcelCell.Text("Citas", ExcelStyles.Header),
            SimpleExcelCell.Text("Ingresos estimados", ExcelStyles.Header),
            SimpleExcelCell.Text("Canceladas", ExcelStyles.Header),
            SimpleExcelCell.Text("No asistidas", ExcelStyles.Header),
            SimpleExcelCell.Text("Ticket promedio", ExcelStyles.Header));

        var citasByDate = citas
            .GroupBy(cita => cita.FechaInicio.Date)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var rowCount = 3;
        for (var date = range.FechaDesde.Date; date <= range.FechaHastaInclusive.Date; date = date.AddDays(1))
        {
            var dayCitas = citasByDate.TryGetValue(date, out var values) ? values : [];
            var incomeCitas = dayCitas.Where(CountsAsEstimatedIncome).ToArray();
            var ingresos = incomeCitas.Sum(cita => cita.PrecioEstimado);

            sheet.AddRow(
                SimpleExcelCell.Date(date),
                SimpleExcelCell.Integer(dayCitas.Length),
                SimpleExcelCell.Currency(ingresos),
                SimpleExcelCell.Integer(dayCitas.Count(IsCancelled)),
                SimpleExcelCell.Integer(dayCitas.Count(IsNoShow)),
                SimpleExcelCell.Currency(incomeCitas.Length == 0 ? 0m : ingresos / incomeCitas.Length));
            rowCount++;
        }

        sheet.AddRow(
            SimpleExcelCell.Text("Total", ExcelStyles.Total),
            SimpleExcelCell.Integer(citas.Count, ExcelStyles.Total),
            SimpleExcelCell.Currency(citas.Where(CountsAsEstimatedIncome).Sum(cita => cita.PrecioEstimado)),
            SimpleExcelCell.Integer(citas.Count(IsCancelled), ExcelStyles.Total),
            SimpleExcelCell.Integer(citas.Count(IsNoShow), ExcelStyles.Total),
            SimpleExcelCell.Currency(BuildMetrics(citas).TicketPromedio));
        sheet.AutoFilterReference = $"A3:F{Math.Max(3, rowCount)}";
    }

    private static void BuildServiciosSheet(
        SimpleExcelWorkbook workbook,
        IReadOnlyCollection<CitaReporteData> citas,
        int top)
    {
        var sheet = workbook.AddWorksheet("Servicios");
        sheet.SetColumnWidths(10, 32, 14, 20, 18, 18, 18, 18);
        AddBandRow(sheet, "Servicios más reservados", 8, ExcelStyles.Title);
        sheet.AddEmptyRow();
        sheet.AddRow(
            SimpleExcelCell.Text("Ranking", ExcelStyles.Header),
            SimpleExcelCell.Text("Servicio", ExcelStyles.Header),
            SimpleExcelCell.Text("Citas", ExcelStyles.Header),
            SimpleExcelCell.Text("Ingresos estimados", ExcelStyles.Header),
            SimpleExcelCell.Text("Ticket promedio", ExcelStyles.Header),
            SimpleExcelCell.Text("Atendidas", ExcelStyles.Header),
            SimpleExcelCell.Text("No asistidas", ExcelStyles.Header),
            SimpleExcelCell.Text("Tasa de inasistencia", ExcelStyles.Header));

        var rowNumber = 1;
        foreach (var row in citas
            .GroupBy(cita => new { cita.IdServicio, cita.Servicio })
            .Select(group =>
            {
                var items = group.ToArray();
                var incomeItems = items.Where(CountsAsEstimatedIncome).ToArray();
                return new
                {
                    group.Key.Servicio,
                    Citas = items.Length,
                    Ingresos = incomeItems.Sum(cita => cita.PrecioEstimado),
                    Ticket = incomeItems.Length == 0 ? 0m : incomeItems.Sum(cita => cita.PrecioEstimado) / incomeItems.Length,
                    Atendidas = items.Count(IsAttended),
                    NoAsistidas = items.Count(IsNoShow),
                    Tasa = CalculateNoShowRate(items)
                };
            })
            .OrderByDescending(item => item.Citas)
            .ThenByDescending(item => item.Ingresos)
            .Take(top))
        {
            sheet.AddRow(
                SimpleExcelCell.Integer(rowNumber),
                SimpleExcelCell.Text(row.Servicio),
                SimpleExcelCell.Integer(row.Citas),
                SimpleExcelCell.Currency(row.Ingresos),
                SimpleExcelCell.Currency(row.Ticket),
                SimpleExcelCell.Integer(row.Atendidas),
                SimpleExcelCell.Integer(row.NoAsistidas),
                SimpleExcelCell.Percent(row.Tasa));
            rowNumber++;
        }

        sheet.AutoFilterReference = $"A3:H{Math.Max(3, rowNumber + 2)}";
    }

    private static void BuildClientesSheet(
        SimpleExcelWorkbook workbook,
        IReadOnlyCollection<CitaReporteData> citas,
        int top)
    {
        var sheet = workbook.AddWorksheet("Clientes frecuentes");
        sheet.SetColumnWidths(10, 32, 34, 14, 20, 16, 16, 18, 18);
        AddBandRow(sheet, "Clientes frecuentes", 9, ExcelStyles.Title);
        sheet.AddEmptyRow();
        sheet.AddRow(
            SimpleExcelCell.Text("Ranking", ExcelStyles.Header),
            SimpleExcelCell.Text("Cliente", ExcelStyles.Header),
            SimpleExcelCell.Text("Email", ExcelStyles.Header),
            SimpleExcelCell.Text("Citas", ExcelStyles.Header),
            SimpleExcelCell.Text("Ingresos estimados", ExcelStyles.Header),
            SimpleExcelCell.Text("Atendidas", ExcelStyles.Header),
            SimpleExcelCell.Text("No asistidas", ExcelStyles.Header),
            SimpleExcelCell.Text("Tasa de inasistencia", ExcelStyles.Header),
            SimpleExcelCell.Text("Última cita", ExcelStyles.Header));

        var rowNumber = 1;
        foreach (var row in citas
            .GroupBy(cita => new { cita.IdCliente, cita.Cliente, cita.ClienteEmail })
            .Select(group =>
            {
                var items = group.ToArray();
                return new
                {
                    group.Key.Cliente,
                    Email = group.Key.ClienteEmail ?? string.Empty,
                    Citas = items.Length,
                    Ingresos = items.Where(CountsAsEstimatedIncome).Sum(cita => cita.PrecioEstimado),
                    Atendidas = items.Count(IsAttended),
                    NoAsistidas = items.Count(IsNoShow),
                    Tasa = CalculateNoShowRate(items),
                    UltimaCita = items.Max(cita => cita.FechaInicio)
                };
            })
            .OrderByDescending(item => item.Citas)
            .ThenByDescending(item => item.Ingresos)
            .Take(top))
        {
            sheet.AddRow(
                SimpleExcelCell.Integer(rowNumber),
                SimpleExcelCell.Text(row.Cliente),
                SimpleExcelCell.Text(row.Email),
                SimpleExcelCell.Integer(row.Citas),
                SimpleExcelCell.Currency(row.Ingresos),
                SimpleExcelCell.Integer(row.Atendidas),
                SimpleExcelCell.Integer(row.NoAsistidas),
                SimpleExcelCell.Percent(row.Tasa),
                SimpleExcelCell.DateTime(row.UltimaCita));
            rowNumber++;
        }

        sheet.AutoFilterReference = $"A3:I{Math.Max(3, rowNumber + 2)}";
    }

    private static void BuildInasistenciaSheet(SimpleExcelWorkbook workbook, IReadOnlyCollection<CitaReporteData> citas)
    {
        var sheet = workbook.AddWorksheet("Inasistencia");
        sheet.SetColumnWidths(30, 16, 16, 16, 18, 22);
        sheet.FrozenRows = 0;
        AddBandRow(sheet, "Tasa de inasistencia", 6, ExcelStyles.Title);
        sheet.AddEmptyRow();

        var metrics = BuildMetrics(citas);
        sheet.AddRow(
            SimpleExcelCell.Text("Indicador", ExcelStyles.Header),
            SimpleExcelCell.Text("Valor", ExcelStyles.Header),
            SimpleExcelCell.Text("Base", ExcelStyles.Header),
            SimpleExcelCell.Blank(ExcelStyles.Header),
            SimpleExcelCell.Blank(ExcelStyles.Header),
            SimpleExcelCell.Blank(ExcelStyles.Header));
        sheet.AddRow(
            SimpleExcelCell.Text("Tasa general"),
            SimpleExcelCell.Percent(metrics.TasaInasistencia),
            SimpleExcelCell.Text("No asistidas / (atendidas + no asistidas)"),
            SimpleExcelCell.Blank(),
            SimpleExcelCell.Blank(),
            SimpleExcelCell.Blank());

        sheet.AddEmptyRow();
        sheet.AddRow(
            SimpleExcelCell.Text("Por servicio", ExcelStyles.Section),
            SimpleExcelCell.Blank(ExcelStyles.Section),
            SimpleExcelCell.Blank(ExcelStyles.Section),
            SimpleExcelCell.Blank(ExcelStyles.Section),
            SimpleExcelCell.Blank(ExcelStyles.Section),
            SimpleExcelCell.Blank(ExcelStyles.Section));
        sheet.AddRow(
            SimpleExcelCell.Text("Servicio", ExcelStyles.Header),
            SimpleExcelCell.Text("Atendidas", ExcelStyles.Header),
            SimpleExcelCell.Text("No asistidas", ExcelStyles.Header),
            SimpleExcelCell.Text("Base", ExcelStyles.Header),
            SimpleExcelCell.Text("Tasa", ExcelStyles.Header),
            SimpleExcelCell.Text("Citas totales", ExcelStyles.Header));

        foreach (var row in citas
            .GroupBy(cita => cita.Servicio)
            .Select(group =>
            {
                var items = group.ToArray();
                var attended = items.Count(IsAttended);
                var noShow = items.Count(IsNoShow);
                var baseCount = attended + noShow;
                return new
                {
                    Servicio = group.Key,
                    Attended = attended,
                    NoShow = noShow,
                    Base = baseCount,
                    Rate = baseCount == 0 ? 0m : (decimal)noShow / baseCount,
                    Total = items.Length
                };
            })
            .OrderByDescending(item => item.Rate)
            .ThenByDescending(item => item.NoShow))
        {
            sheet.AddRow(
                SimpleExcelCell.Text(row.Servicio),
                SimpleExcelCell.Integer(row.Attended),
                SimpleExcelCell.Integer(row.NoShow),
                SimpleExcelCell.Integer(row.Base),
                SimpleExcelCell.Percent(row.Rate),
                SimpleExcelCell.Integer(row.Total));
        }

        sheet.AddEmptyRow();
        sheet.AddRow(
            SimpleExcelCell.Text("Por prestador", ExcelStyles.Section),
            SimpleExcelCell.Blank(ExcelStyles.Section),
            SimpleExcelCell.Blank(ExcelStyles.Section),
            SimpleExcelCell.Blank(ExcelStyles.Section),
            SimpleExcelCell.Blank(ExcelStyles.Section),
            SimpleExcelCell.Blank(ExcelStyles.Section));
        sheet.AddRow(
            SimpleExcelCell.Text("Prestador", ExcelStyles.Header),
            SimpleExcelCell.Text("Atendidas", ExcelStyles.Header),
            SimpleExcelCell.Text("No asistidas", ExcelStyles.Header),
            SimpleExcelCell.Text("Base", ExcelStyles.Header),
            SimpleExcelCell.Text("Tasa", ExcelStyles.Header),
            SimpleExcelCell.Text("Citas totales", ExcelStyles.Header));

        foreach (var row in citas
            .GroupBy(cita => cita.Prestador ?? "Sin prestador")
            .Select(group =>
            {
                var items = group.ToArray();
                var attended = items.Count(IsAttended);
                var noShow = items.Count(IsNoShow);
                var baseCount = attended + noShow;
                return new
                {
                    Prestador = group.Key,
                    Attended = attended,
                    NoShow = noShow,
                    Base = baseCount,
                    Rate = baseCount == 0 ? 0m : (decimal)noShow / baseCount,
                    Total = items.Length
                };
            })
            .OrderByDescending(item => item.Rate)
            .ThenByDescending(item => item.NoShow))
        {
            sheet.AddRow(
                SimpleExcelCell.Text(row.Prestador),
                SimpleExcelCell.Integer(row.Attended),
                SimpleExcelCell.Integer(row.NoShow),
                SimpleExcelCell.Integer(row.Base),
                SimpleExcelCell.Percent(row.Rate),
                SimpleExcelCell.Integer(row.Total));
        }
    }

    private static void BuildPrestadoresSheet(
        SimpleExcelWorkbook workbook,
        IReadOnlyCollection<CitaReporteData> citas,
        IReadOnlyCollection<PrestadorReporteData> prestadores)
    {
        var sheet = workbook.AddWorksheet("Prestadores");
        sheet.SetColumnWidths(30, 20, 12, 14, 14, 14, 14, 20, 20, 18);
        AddBandRow(sheet, "Rendimiento por prestador", 10, ExcelStyles.Title);
        sheet.AddEmptyRow();
        sheet.AddRow(
            SimpleExcelCell.Text("Prestador", ExcelStyles.Header),
            SimpleExcelCell.Text("Tipo", ExcelStyles.Header),
            SimpleExcelCell.Text("Activo", ExcelStyles.Header),
            SimpleExcelCell.Text("Citas", ExcelStyles.Header),
            SimpleExcelCell.Text("Atendidas", ExcelStyles.Header),
            SimpleExcelCell.Text("Canceladas", ExcelStyles.Header),
            SimpleExcelCell.Text("No asistidas", ExcelStyles.Header),
            SimpleExcelCell.Text("Tasa de inasistencia", ExcelStyles.Header),
            SimpleExcelCell.Text("Ingresos estimados", ExcelStyles.Header),
            SimpleExcelCell.Text("Minutos reservados", ExcelStyles.Header));

        var citasByProvider = citas
            .Where(cita => cita.IdPrestador.HasValue)
            .GroupBy(cita => cita.IdPrestador!.Value)
            .ToDictionary(group => group.Key, group => group.ToArray());

        foreach (var prestador in prestadores.OrderBy(item => item.Nombre))
        {
            var items = citasByProvider.TryGetValue(prestador.IdPrestador, out var values) ? values : [];
            if (!prestador.Activo && items.Length == 0)
            {
                continue;
            }

            sheet.AddRow(
                SimpleExcelCell.Text(prestador.Nombre),
                SimpleExcelCell.Text(prestador.TipoPrestador),
                SimpleExcelCell.Text(prestador.Activo ? "Sí" : "No"),
                SimpleExcelCell.Integer(items.Length),
                SimpleExcelCell.Integer(items.Count(IsAttended)),
                SimpleExcelCell.Integer(items.Count(IsCancelled)),
                SimpleExcelCell.Integer(items.Count(IsNoShow)),
                SimpleExcelCell.Percent(CalculateNoShowRate(items)),
                SimpleExcelCell.Currency(items.Where(CountsAsEstimatedIncome).Sum(cita => cita.PrecioEstimado)),
                SimpleExcelCell.Integer(items.Where(CountsAsEstimatedIncome).Sum(GetDurationMinutes)));
        }

        sheet.AutoFilterReference = $"A3:J{Math.Max(3, sheet.Rows.Count)}";
    }

    private static void BuildDetalleSheet(SimpleExcelWorkbook workbook, IReadOnlyCollection<CitaReporteData> citas)
    {
        var sheet = workbook.AddWorksheet("Detalle citas");
        sheet.SetColumnWidths(14, 18, 20, 20, 30, 30, 30, 24, 18, 20, 20);
        AddBandRow(sheet, "Detalle de citas", 11, ExcelStyles.Title);
        sheet.AddEmptyRow();
        sheet.AddRow(
            SimpleExcelCell.Text("Código", ExcelStyles.Header),
            SimpleExcelCell.Text("Fecha inicio", ExcelStyles.Header),
            SimpleExcelCell.Text("Fecha fin", ExcelStyles.Header),
            SimpleExcelCell.Text("Estado", ExcelStyles.Header),
            SimpleExcelCell.Text("Cliente", ExcelStyles.Header),
            SimpleExcelCell.Text("Email cliente", ExcelStyles.Header),
            SimpleExcelCell.Text("Servicio", ExcelStyles.Header),
            SimpleExcelCell.Text("Prestador", ExcelStyles.Header),
            SimpleExcelCell.Text("Precio estimado", ExcelStyles.Header),
            SimpleExcelCell.Text("Estado final", ExcelStyles.Header),
            SimpleExcelCell.Text("Duración minutos", ExcelStyles.Header));

        foreach (var cita in citas.OrderBy(item => item.FechaInicio))
        {
            sheet.AddRow(
                SimpleExcelCell.Text(cita.Codigo),
                SimpleExcelCell.DateTime(cita.FechaInicio),
                SimpleExcelCell.DateTime(cita.FechaFin),
                SimpleExcelCell.Text(cita.Estado),
                SimpleExcelCell.Text(cita.Cliente),
                SimpleExcelCell.Text(cita.ClienteEmail),
                SimpleExcelCell.Text(cita.Servicio),
                SimpleExcelCell.Text(cita.Prestador ?? "Sin prestador"),
                SimpleExcelCell.Currency(cita.PrecioEstimado),
                SimpleExcelCell.Text(cita.EsEstadoFinal ? "Sí" : "No"),
                SimpleExcelCell.Integer(GetDurationMinutes(cita)));
        }

        sheet.AutoFilterReference = $"A3:K{Math.Max(3, sheet.Rows.Count)}";
    }

    private static ReportMetrics BuildMetrics(IReadOnlyCollection<CitaReporteData> citas)
    {
        var incomeCitas = citas.Where(CountsAsEstimatedIncome).ToArray();
        var ingresos = incomeCitas.Sum(cita => cita.PrecioEstimado);

        return new ReportMetrics(
            citas.Count,
            citas.Count(IsPending),
            citas.Count(IsConfirmed),
            citas.Count(IsCancelled),
            citas.Count(IsAttended),
            citas.Count(IsNoShow),
            ingresos,
            incomeCitas.Length == 0 ? 0m : ingresos / incomeCitas.Length,
            CalculateNoShowRate(citas),
            citas.Select(cita => cita.IdCliente).Distinct().Count(),
            citas.Select(cita => cita.IdServicio).Distinct().Count(),
            citas.Where(cita => cita.IdPrestador.HasValue).Select(cita => cita.IdPrestador!.Value).Distinct().Count());
    }

    private static void AddBandRow(SimpleExcelWorksheet sheet, string text, int columns, ExcelStyles style)
    {
        var cells = new SimpleExcelCell[columns];
        cells[0] = SimpleExcelCell.Text(text, style);

        for (var index = 1; index < columns; index++)
        {
            cells[index] = SimpleExcelCell.Blank(style);
        }

        sheet.AddRow(cells);
    }

    private static ReportRange NormalizeRange(ReporteNegocioQuery query)
    {
        var today = DateTime.Today;
        var fechaDesde = (query.FechaDesde ?? new DateTime(today.Year, today.Month, 1)).Date;
        var fechaHastaInclusive = (query.FechaHasta ?? today).Date;

        if (fechaHastaInclusive < fechaDesde)
        {
            return ReportRange.Invalid(nameof(query.FechaHasta), "La fecha hasta debe ser mayor o igual a la fecha desde.");
        }

        if ((fechaHastaInclusive - fechaDesde).Days + 1 > MaxRangeDays)
        {
            return ReportRange.Invalid(nameof(query.FechaHasta), $"El rango de reportes no puede superar {MaxRangeDays} días.");
        }

        return ReportRange.Valid(fechaDesde, fechaHastaInclusive);
    }

    private static string BuildFileName(NegocioReporteData negocio, ReportRange range)
    {
        var slug = SanitizeFileName(string.IsNullOrWhiteSpace(negocio.Slug) ? negocio.Nombre : negocio.Slug);
        return $"reporte-negocio-{slug}-{range.FechaDesde:yyyyMMdd}-{range.FechaHastaInclusive:yyyyMMdd}.xlsx";
    }

    private static string SanitizeFileName(string value)
    {
        var normalized = RemoveDiacritics(value).ToLowerInvariant();
        var builder = new StringBuilder();
        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                continue;
            }

            if (character is '-' or '_' or ' ')
            {
                builder.Append('-');
            }
        }

        var result = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(result) ? "negocio" : result;
    }

    private static bool CountsAsEstimatedIncome(CitaReporteData cita)
    {
        return !IsCancelled(cita) && !IsNoShow(cita);
    }

    private static bool IsPending(CitaReporteData cita)
    {
        return NormalizeState(cita.Estado) == PendingStateName;
    }

    private static bool IsConfirmed(CitaReporteData cita)
    {
        return NormalizeState(cita.Estado) == ConfirmedStateName;
    }

    private static bool IsCancelled(CitaReporteData cita)
    {
        return NormalizeState(cita.Estado) == CancelledStateName;
    }

    private static bool IsAttended(CitaReporteData cita)
    {
        return NormalizeState(cita.Estado) == AttendedStateName;
    }

    private static bool IsNoShow(CitaReporteData cita)
    {
        return NormalizeState(cita.Estado) == NoShowStateName;
    }

    private static decimal CalculateNoShowRate(IReadOnlyCollection<CitaReporteData> citas)
    {
        var attended = citas.Count(IsAttended);
        var noShow = citas.Count(IsNoShow);
        var denominator = attended + noShow;

        return denominator == 0 ? 0m : (decimal)noShow / denominator;
    }

    private static int GetDurationMinutes(CitaReporteData cita)
    {
        return Math.Max(0, (int)Math.Round((cita.FechaFin - cita.FechaInicio).TotalMinutes));
    }

    private static string NormalizeState(string value)
    {
        return RemoveDiacritics(value).Trim().ToUpperInvariant();
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(capacity: normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private sealed record NegocioReporteData(
        int IdNegocio,
        string Nombre,
        string Slug,
        string Rubro);

    private sealed record CitaReporteData(
        int IdCita,
        string Codigo,
        DateTime FechaInicio,
        DateTime FechaFin,
        int IdCliente,
        string Cliente,
        string? ClienteEmail,
        int IdServicio,
        string Servicio,
        int? IdPrestador,
        string? Prestador,
        int IdEstadoCita,
        string Estado,
        bool EsEstadoFinal,
        decimal PrecioEstimado);

    private sealed record PrestadorReporteData(
        int IdPrestador,
        string Nombre,
        string TipoPrestador,
        bool Activo);

    private sealed record ReportMetrics(
        int TotalCitas,
        int CitasPendientes,
        int CitasConfirmadas,
        int CitasCanceladas,
        int CitasAtendidas,
        int CitasNoAsistidas,
        decimal IngresosEstimados,
        decimal TicketPromedio,
        decimal TasaInasistencia,
        int ClientesUnicos,
        int ServiciosReservados,
        int PrestadoresConCitas);

    private sealed record ReportRange(
        bool IsValid,
        DateTime FechaDesde,
        DateTime FechaHastaInclusive,
        DateTime FechaHastaExclusive,
        string? Field,
        string? Error)
    {
        public static ReportRange Valid(DateTime fechaDesde, DateTime fechaHastaInclusive)
        {
            return new ReportRange(true, fechaDesde, fechaHastaInclusive, fechaHastaInclusive.AddDays(1), null, null);
        }

        public static ReportRange Invalid(string field, string error)
        {
            return new ReportRange(false, default, default, default, field, error);
        }
    }
}
