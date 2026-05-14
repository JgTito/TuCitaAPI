using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TuCita.Application.Common;
using TuCita.Application.InformesInteligentes;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.InformesInteligentes;

public sealed class InformeInteligenteService(
    ReservaFlowDbContext dbContext,
    IInformeInteligenteAiClient aiClient) : IInformeInteligenteService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";
    private const int MaxRangeDays = 366;
    private const string PendingStateName = "PENDIENTE";
    private const string ConfirmedStateName = "CONFIRMADA";
    private const string AttendedStateName = "ATENDIDA";
    private const string CancelledStateName = "CANCELADA";
    private const string NoShowStateName = "NO ASISTIO";

    public async Task<ServiceResult<InformeInteligenteContextoDto>> GetContextoAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        InformeInteligenteQuery query,
        CancellationToken cancellationToken)
    {
        var negocio = await dbContext.Negocios
            .AsNoTracking()
            .Where(item => item.IdNegocio == idNegocio)
            .Select(item => new NegocioInformeData(
                item.IdNegocio,
                item.Nombre,
                item.Slug,
                item.Rubro.Nombre,
                item.Activo))
            .FirstOrDefaultAsync(cancellationToken);

        if (negocio is null)
        {
            return ServiceResult<InformeInteligenteContextoDto>.NotFound("El negocio no existe.");
        }

        if (!await CanAccessAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<InformeInteligenteContextoDto>.Forbidden("No tienes acceso para generar informes inteligentes de este negocio.");
        }

        var range = NormalizeRange(query);
        if (!range.IsValid)
        {
            return ServiceResult<InformeInteligenteContextoDto>.Validation([
                new ValidationError(range.Field ?? string.Empty, range.Error!)
            ]);
        }

        var top = Math.Clamp(query.Top, 1, 50);
        var currentCitas = await LoadCitasAsync(idNegocio, range.FechaDesde, range.FechaHastaExclusive, cancellationToken);
        var servicios = await LoadServiciosAsync(idNegocio, cancellationToken);
        var prestadores = await LoadPrestadoresAsync(idNegocio, cancellationToken);
        var horariosPrestador = await LoadHorariosPrestadorAsync(idNegocio, cancellationToken);
        var firstAppointmentByClient = await LoadFirstAppointmentByClientAsync(idNegocio, currentCitas, cancellationToken);

        var availableHoursByProvider = BuildAvailableHoursByProvider(prestadores, horariosPrestador, range);
        var indicators = BuildIndicators(currentCitas, firstAppointmentByClient, availableHoursByProvider);
        var previousRange = BuildPreviousRange(range);
        InformeInteligenteComparacionDto? comparison = null;

        if (query.CompararPeriodoAnterior)
        {
            var previousCitas = await LoadCitasAsync(idNegocio, previousRange.FechaDesde, previousRange.FechaHastaExclusive, cancellationToken);
            var previousFirstAppointments = await LoadFirstAppointmentByClientAsync(idNegocio, previousCitas, cancellationToken);
            var previousAvailableHours = BuildAvailableHoursByProvider(prestadores, horariosPrestador, previousRange);
            var previousIndicators = BuildIndicators(previousCitas, previousFirstAppointments, previousAvailableHours);

            comparison = BuildComparison(previousRange, indicators, previousIndicators);
        }

        var serviceRows = BuildServiceRows(servicios, currentCitas);
        var providerRows = BuildProviderRows(prestadores, currentCitas, availableHoursByProvider);
        var hourlyRows = BuildHourlyRows(horariosPrestador, currentCitas);
        var dayRows = BuildDayRows(currentCitas);
        var clientSegments = BuildClientSegments(currentCitas, firstAppointmentByClient);
        var quality = BuildQualityData(currentCitas, prestadores, horariosPrestador);

        var dto = new InformeInteligenteContextoDto(
            new InformeInteligenteNegocioDto(negocio.IdNegocio, negocio.Nombre, negocio.Slug, negocio.Rubro, negocio.Activo),
            ToPeriodoDto(range),
            indicators,
            comparison,
            BuildStateRows(currentCitas),
            serviceRows.OrderByDescending(item => item.TotalCitas).ThenByDescending(item => item.IngresosEstimados).Take(top).ToArray(),
            serviceRows.OrderBy(item => item.TotalCitas).ThenBy(item => item.Servicio).Take(top).ToArray(),
            serviceRows.OrderByDescending(item => item.IngresosEstimados).ThenByDescending(item => item.TotalCitas).Take(top).ToArray(),
            providerRows.OrderByDescending(item => item.HorasReservadas).ThenByDescending(item => item.TotalCitas).Take(top).ToArray(),
            providerRows.OrderBy(item => item.TasaOcupacion).ThenBy(item => item.Prestador).Take(top).ToArray(),
            hourlyRows.OrderByDescending(item => item.TotalCitas).ThenByDescending(item => item.HorasReservadas).Take(top).ToArray(),
            hourlyRows.OrderBy(item => item.TotalCitas).ThenBy(item => item.HoraInicio).Take(top).ToArray(),
            dayRows.OrderByDescending(item => item.TotalCitas).ThenBy(item => item.DiaSemana).Take(top).ToArray(),
            dayRows.OrderByDescending(item => item.CitasCanceladas).ThenByDescending(item => item.CitasNoAsistidas).Take(top).ToArray(),
            clientSegments,
            quality,
            query.IncluirPromptSugerido ? BuildPrompt(negocio, range) : null);

        return ServiceResult<InformeInteligenteContextoDto>.Success(dto);
    }

    public async Task<ServiceResult<InformeInteligenteArchivoDto>> DescargarPdfAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        InformeInteligenteQuery query,
        CancellationToken cancellationToken)
    {
        var contextoResult = await GetContextoAsync(currentUser, idNegocio, query, cancellationToken);
        if (!contextoResult.Succeeded || contextoResult.Data is null)
        {
            return ConvertError<InformeInteligenteContextoDto, InformeInteligenteArchivoDto>(contextoResult);
        }

        var informeResult = await aiClient.GenerarInformeAsync(contextoResult.Data, cancellationToken);
        if (!informeResult.Succeeded || informeResult.Data is null)
        {
            return ConvertError<InformeAiGenerationResult, InformeInteligenteArchivoDto>(informeResult);
        }

        var content = InformeInteligentePdfBuilder.Build(contextoResult.Data, informeResult.Data);
        var fileName = InformeInteligentePdfBuilder.BuildFileName(contextoResult.Data);

        return ServiceResult<InformeInteligenteArchivoDto>.Success(
            new InformeInteligenteArchivoDto(fileName, "application/pdf", content));
    }

    private static ServiceResult<TDestination> ConvertError<TSource, TDestination>(ServiceResult<TSource> result)
    {
        return result.Status switch
        {
            ServiceResultStatus.NotFound => ServiceResult<TDestination>.NotFound(result.Errors.FirstOrDefault() ?? "No encontrado."),
            ServiceResultStatus.Forbidden => ServiceResult<TDestination>.Forbidden(result.Errors.FirstOrDefault() ?? "No autorizado."),
            ServiceResultStatus.Validation => ServiceResult<TDestination>.Validation(result.ValidationErrors),
            _ => ServiceResult<TDestination>.Validation(result.Errors)
        };
    }

    private async Task<bool> CanAccessAsync(
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

    private async Task<CitaInformeData[]> LoadCitasAsync(
        int idNegocio,
        DateTime fechaDesde,
        DateTime fechaHastaExclusive,
        CancellationToken cancellationToken)
    {
        return await dbContext.Citas
            .AsNoTracking()
            .Where(cita =>
                cita.IdNegocio == idNegocio &&
                cita.FechaInicio >= fechaDesde &&
                cita.FechaInicio < fechaHastaExclusive)
            .Select(cita => new CitaInformeData(
                cita.IdCita,
                cita.Codigo,
                cita.IdCliente,
                cita.Cliente.Nombre,
                cita.IdServicio,
                cita.Servicio.Nombre,
                cita.IdPrestador,
                cita.Prestador == null ? null : cita.Prestador.Nombre,
                cita.IdEstadoCita,
                cita.EstadoCita.Nombre,
                cita.EstadoCita.EsEstadoFinal,
                cita.FechaInicio,
                cita.FechaFin,
                cita.FechaCreacion,
                cita.PrecioEstimado))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<ServicioInformeData[]> LoadServiciosAsync(int idNegocio, CancellationToken cancellationToken)
    {
        return await dbContext.Servicios
            .AsNoTracking()
            .Where(servicio => servicio.IdNegocio == idNegocio && servicio.Activo)
            .Select(servicio => new ServicioInformeData(
                servicio.IdServicio,
                servicio.Nombre,
                servicio.DuracionMinutos,
                servicio.Precio))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<PrestadorInformeData[]> LoadPrestadoresAsync(int idNegocio, CancellationToken cancellationToken)
    {
        return await dbContext.Prestadores
            .AsNoTracking()
            .Where(prestador => prestador.IdNegocio == idNegocio && prestador.Activo)
            .Select(prestador => new PrestadorInformeData(
                prestador.IdPrestador,
                prestador.Nombre,
                prestador.Capacidad))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<HorarioPrestadorInformeData[]> LoadHorariosPrestadorAsync(
        int idNegocio,
        CancellationToken cancellationToken)
    {
        return await dbContext.HorariosPrestador
            .AsNoTracking()
            .Where(horario => horario.IdNegocio == idNegocio && horario.Activo)
            .Select(horario => new HorarioPrestadorInformeData(
                horario.IdPrestador,
                horario.DiaSemana,
                horario.HoraInicio,
                horario.HoraFin))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<IReadOnlyDictionary<int, DateTime>> LoadFirstAppointmentByClientAsync(
        int idNegocio,
        IReadOnlyCollection<CitaInformeData> citas,
        CancellationToken cancellationToken)
    {
        var clientIds = citas.Select(cita => cita.IdCliente).Distinct().ToArray();
        if (clientIds.Length == 0)
        {
            return new Dictionary<int, DateTime>();
        }

        return await dbContext.Citas
            .AsNoTracking()
            .Where(cita => cita.IdNegocio == idNegocio && clientIds.Contains(cita.IdCliente))
            .GroupBy(cita => cita.IdCliente)
            .Select(group => new { IdCliente = group.Key, FechaPrimeraCita = group.Min(cita => cita.FechaInicio) })
            .ToDictionaryAsync(item => item.IdCliente, item => item.FechaPrimeraCita, cancellationToken);
    }

    private static InformeInteligenteIndicadoresDto BuildIndicators(
        IReadOnlyCollection<CitaInformeData> citas,
        IReadOnlyDictionary<int, DateTime> firstAppointmentByClient,
        IReadOnlyDictionary<int, decimal> availableHoursByProvider)
    {
        var total = citas.Count;
        var atendidas = citas.Count(IsAttended);
        var canceladas = citas.Count(IsCancelled);
        var noAsistidas = citas.Count(IsNoShow);
        var pendientes = citas.Count(IsPending);
        var confirmadas = citas.Count(IsConfirmed);
        var incomeCitas = citas.Where(CountsAsEstimatedIncome).ToArray();
        var ingresos = incomeCitas.Sum(cita => cita.PrecioEstimado);
        var clientesUnicos = citas.Select(cita => cita.IdCliente).Distinct().Count();
        var currentStart = citas.Count == 0 ? DateTime.MaxValue : citas.Min(cita => cita.FechaInicio);
        var newClientIds = firstAppointmentByClient
            .Where(item => item.Value >= currentStart)
            .Select(item => item.Key)
            .ToHashSet();
        var citasClientesNuevos = citas.Where(cita => newClientIds.Contains(cita.IdCliente)).ToArray();
        var citasClientesRecurrentes = citas.Where(cita => !newClientIds.Contains(cita.IdCliente)).ToArray();
        var horasReservadas = citas
            .Where(cita => !IsCancelled(cita))
            .Sum(GetDurationHours);
        var horasDisponibles = availableHoursByProvider.Values.Sum();
        var horasEntreReservaAtencion = citas
            .Where(cita => cita.FechaInicio > cita.FechaCreacion)
            .Select(cita => (decimal)(cita.FechaInicio - cita.FechaCreacion).TotalHours)
            .ToArray();

        return new InformeInteligenteIndicadoresDto(
            total,
            pendientes,
            confirmadas,
            atendidas,
            canceladas,
            noAsistidas,
            Rate(atendidas, Math.Max(1, atendidas + noAsistidas + canceladas)),
            Rate(canceladas, total),
            Rate(noAsistidas, Math.Max(1, atendidas + noAsistidas)),
            Rate(canceladas + noAsistidas, total),
            RoundMoney(ingresos),
            RoundMoney(incomeCitas.Length == 0 ? 0 : ingresos / incomeCitas.Length),
            clientesUnicos,
            citasClientesNuevos.Select(cita => cita.IdCliente).Distinct().Count(),
            citasClientesRecurrentes.Select(cita => cita.IdCliente).Distinct().Count(),
            CalculateNoShowRate(citasClientesNuevos),
            CalculateNoShowRate(citasClientesRecurrentes),
            RoundTwo(horasReservadas),
            RoundTwo(horasDisponibles),
            Rate(horasReservadas, horasDisponibles),
            RoundTwo(horasEntreReservaAtencion.Length == 0 ? 0 : horasEntreReservaAtencion.Average()));
    }

    private static InformeInteligenteComparacionDto BuildComparison(
        InformeRange previousRange,
        InformeInteligenteIndicadoresDto current,
        InformeInteligenteIndicadoresDto previous)
    {
        return new InformeInteligenteComparacionDto(
            ToPeriodoDto(previousRange),
            previous.TotalCitas,
            Variation(current.TotalCitas, previous.TotalCitas),
            previous.IngresosEstimados,
            Variation(current.IngresosEstimados, previous.IngresosEstimados),
            previous.TasaAsistencia,
            current.TasaAsistencia - previous.TasaAsistencia,
            previous.TasaCancelacionNoAsistencia,
            current.TasaCancelacionNoAsistencia - previous.TasaCancelacionNoAsistencia,
            previous.TasaOcupacionAgenda,
            current.TasaOcupacionAgenda - previous.TasaOcupacionAgenda);
    }

    private static IReadOnlyCollection<InformeInteligenteEstadoDto> BuildStateRows(IReadOnlyCollection<CitaInformeData> citas)
    {
        var total = Math.Max(1, citas.Count);
        return citas
            .GroupBy(cita => new { cita.IdEstadoCita, cita.Estado, cita.EsEstadoFinal })
            .OrderBy(group => group.Key.Estado)
            .Select(group => new InformeInteligenteEstadoDto(
                group.Key.IdEstadoCita,
                group.Key.Estado,
                group.Key.EsEstadoFinal,
                group.Count(),
                Rate(group.Count(), total)))
            .ToArray();
    }

    private static IReadOnlyCollection<InformeInteligenteServicioDto> BuildServiceRows(
        IReadOnlyCollection<ServicioInformeData> servicios,
        IReadOnlyCollection<CitaInformeData> citas)
    {
        var total = Math.Max(1, citas.Count);
        var citasByServicio = citas
            .GroupBy(cita => cita.IdServicio)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var serviceIds = servicios.Select(servicio => servicio.IdServicio).ToHashSet();
        var rows = servicios
            .Select(servicio =>
            {
                var serviceCitas = citasByServicio.TryGetValue(servicio.IdServicio, out var values)
                    ? values
                    : [];

                return BuildServiceRow(
                    servicio.IdServicio,
                    servicio.Nombre,
                    servicio.DuracionMinutos,
                    serviceCitas,
                    total);
            })
            .ToList();

        rows.AddRange(citas
            .Where(cita => !serviceIds.Contains(cita.IdServicio))
            .GroupBy(cita => new { cita.IdServicio, cita.Servicio })
            .Select(group => BuildServiceRow(group.Key.IdServicio, group.Key.Servicio, 0, group.ToArray(), total)));

        return rows;
    }

    private static InformeInteligenteServicioDto BuildServiceRow(
        int idServicio,
        string servicio,
        int duracionBase,
        IReadOnlyCollection<CitaInformeData> citas,
        int totalCitasPeriodo)
    {
        var incomeCitas = citas.Where(CountsAsEstimatedIncome).ToArray();
        var ingresos = incomeCitas.Sum(cita => cita.PrecioEstimado);
        var durations = citas.Select(GetDurationMinutes).Where(duration => duration > 0).ToArray();

        return new InformeInteligenteServicioDto(
            idServicio,
            servicio,
            citas.Count,
            Rate(citas.Count, totalCitasPeriodo),
            citas.Count(IsAttended),
            citas.Count(IsCancelled),
            citas.Count(IsNoShow),
            CalculateNoShowRate(citas),
            RoundMoney(ingresos),
            RoundMoney(incomeCitas.Length == 0 ? 0 : ingresos / incomeCitas.Length),
            RoundTwo(durations.Length == 0 ? duracionBase : (decimal)durations.Average()));
    }

    private static IReadOnlyCollection<InformeInteligentePrestadorDto> BuildProviderRows(
        IReadOnlyCollection<PrestadorInformeData> prestadores,
        IReadOnlyCollection<CitaInformeData> citas,
        IReadOnlyDictionary<int, decimal> availableHoursByProvider)
    {
        var citasByProvider = citas
            .Where(cita => cita.IdPrestador.HasValue)
            .GroupBy(cita => cita.IdPrestador!.Value)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var rows = prestadores.Select(prestador =>
        {
            var providerCitas = citasByProvider.TryGetValue(prestador.IdPrestador, out var values)
                ? values
                : [];
            var horasReservadas = providerCitas
                .Where(cita => !IsCancelled(cita))
                .Sum(GetDurationHours);
            var horasDisponibles = availableHoursByProvider.TryGetValue(prestador.IdPrestador, out var available)
                ? available
                : 0m;

            return new InformeInteligentePrestadorDto(
                prestador.IdPrestador,
                prestador.Nombre,
                providerCitas.Length,
                providerCitas.Count(IsAttended),
                providerCitas.Count(IsCancelled),
                providerCitas.Count(IsNoShow),
                RoundTwo(horasReservadas),
                RoundTwo(horasDisponibles),
                Rate(horasReservadas, horasDisponibles),
                RoundMoney(providerCitas.Where(CountsAsEstimatedIncome).Sum(cita => cita.PrecioEstimado)));
        }).ToList();

        var unassigned = citas.Where(cita => !cita.IdPrestador.HasValue).ToArray();
        if (unassigned.Length > 0)
        {
            rows.Add(new InformeInteligentePrestadorDto(
                null,
                "Sin prestador asignado",
                unassigned.Length,
                unassigned.Count(IsAttended),
                unassigned.Count(IsCancelled),
                unassigned.Count(IsNoShow),
                RoundTwo(unassigned.Where(cita => !IsCancelled(cita)).Sum(GetDurationHours)),
                0,
                0,
                RoundMoney(unassigned.Where(CountsAsEstimatedIncome).Sum(cita => cita.PrecioEstimado))));
        }

        return rows;
    }

    private static IReadOnlyCollection<InformeInteligenteHorarioDto> BuildHourlyRows(
        IReadOnlyCollection<HorarioPrestadorInformeData> horarios,
        IReadOnlyCollection<CitaInformeData> citas)
    {
        var scheduledHours = horarios
            .SelectMany(GetScheduledHours)
            .Distinct()
            .ToHashSet();
        var appointmentStartHours = citas.Select(cita => cita.FechaInicio.Hour).Distinct();
        foreach (var hour in appointmentStartHours)
        {
            scheduledHours.Add(hour);
        }

        var reservedHoursByBlock = new Dictionary<int, decimal>();
        foreach (var cita in citas.Where(cita => !IsCancelled(cita)))
        {
            AddDurationByHour(reservedHoursByBlock, cita.FechaInicio, cita.FechaFin);
        }

        return scheduledHours
            .OrderBy(hour => hour)
            .Select(hour =>
            {
                var blockCitas = citas.Where(cita => cita.FechaInicio.Hour == hour).ToArray();
                return new InformeInteligenteHorarioDto(
                    hour,
                    $"{hour:00}:00 - {(hour + 1) % 24:00}:00",
                    blockCitas.Length,
                    blockCitas.Count(IsAttended),
                    blockCitas.Count(IsCancelled),
                    blockCitas.Count(IsNoShow),
                    RoundTwo(reservedHoursByBlock.GetValueOrDefault(hour)),
                    RoundMoney(blockCitas.Where(CountsAsEstimatedIncome).Sum(cita => cita.PrecioEstimado)));
            })
            .ToArray();
    }

    private static IReadOnlyCollection<InformeInteligenteDiaDto> BuildDayRows(IReadOnlyCollection<CitaInformeData> citas)
    {
        return Enumerable.Range(1, 7)
            .Select(day =>
            {
                var dayCitas = citas.Where(cita => ToBusinessDayOfWeek(cita.FechaInicio) == day).ToArray();
                return new InformeInteligenteDiaDto(
                    day,
                    GetDayName(day),
                    dayCitas.Length,
                    dayCitas.Count(IsCancelled),
                    dayCitas.Count(IsNoShow),
                    RoundMoney(dayCitas.Where(CountsAsEstimatedIncome).Sum(cita => cita.PrecioEstimado)));
            })
            .ToArray();
    }

    private static IReadOnlyCollection<InformeInteligenteClienteSegmentoDto> BuildClientSegments(
        IReadOnlyCollection<CitaInformeData> citas,
        IReadOnlyDictionary<int, DateTime> firstAppointmentByClient)
    {
        if (citas.Count == 0)
        {
            return [];
        }

        var currentStart = citas.Min(cita => cita.FechaInicio);
        return new[]
        {
            BuildClientSegment("Clientes nuevos", citas.Where(cita =>
                firstAppointmentByClient.TryGetValue(cita.IdCliente, out var firstDate) && firstDate >= currentStart).ToArray()),
            BuildClientSegment("Clientes recurrentes", citas.Where(cita =>
                !firstAppointmentByClient.TryGetValue(cita.IdCliente, out var firstDate) || firstDate < currentStart).ToArray())
        };
    }

    private static InformeInteligenteClienteSegmentoDto BuildClientSegment(
        string segmento,
        IReadOnlyCollection<CitaInformeData> citas)
    {
        return new InformeInteligenteClienteSegmentoDto(
            segmento,
            citas.Count,
            citas.Select(cita => cita.IdCliente).Distinct().Count(),
            citas.Count(IsAttended),
            citas.Count(IsCancelled),
            citas.Count(IsNoShow),
            CalculateNoShowRate(citas),
            RoundMoney(citas.Where(CountsAsEstimatedIncome).Sum(cita => cita.PrecioEstimado)));
    }

    private static InformeInteligenteCalidadDatosDto BuildQualityData(
        IReadOnlyCollection<CitaInformeData> citas,
        IReadOnlyCollection<PrestadorInformeData> prestadores,
        IReadOnlyCollection<HorarioPrestadorInformeData> horarios)
    {
        var prestadoresConHorario = horarios.Select(horario => horario.IdPrestador).Distinct().ToHashSet();
        var prestadoresSinHorario = prestadores.Count(prestador => !prestadoresConHorario.Contains(prestador.IdPrestador));
        var citasSinPrestador = citas.Count(cita => !cita.IdPrestador.HasValue);
        var warnings = new List<string>();

        if (citas.Count < 10)
        {
            warnings.Add("El período tiene pocas citas; las conclusiones de la IA deben presentarse como señales preliminares.");
        }

        if (prestadoresSinHorario > 0)
        {
            warnings.Add("Hay prestadores activos sin horario configurado; la ocupación puede estar subestimada.");
        }

        if (citasSinPrestador > 0)
        {
            warnings.Add("Hay citas sin prestador asignado; el análisis de carga por prestador puede quedar incompleto.");
        }

        return new InformeInteligenteCalidadDatosDto(
            citas.Count >= 10,
            citasSinPrestador,
            prestadoresSinHorario,
            warnings);
    }

    private static IReadOnlyDictionary<int, decimal> BuildAvailableHoursByProvider(
        IReadOnlyCollection<PrestadorInformeData> prestadores,
        IReadOnlyCollection<HorarioPrestadorInformeData> horarios,
        InformeRange range)
    {
        var capacityByProvider = prestadores.ToDictionary(item => item.IdPrestador, item => Math.Max(1, item.Capacidad));
        var result = prestadores.ToDictionary(item => item.IdPrestador, _ => 0m);
        var dates = EachDate(range.FechaDesde.Date, range.FechaHastaInclusive.Date).ToArray();

        foreach (var horario in horarios)
        {
            if (!result.ContainsKey(horario.IdPrestador))
            {
                continue;
            }

            var occurrences = dates.Count(date => ToBusinessDayOfWeek(date) == horario.DiaSemana);
            var minutes = Math.Max(0, (decimal)(horario.HoraFin.ToTimeSpan() - horario.HoraInicio.ToTimeSpan()).TotalMinutes);
            result[horario.IdPrestador] += occurrences * minutes / 60m * capacityByProvider.GetValueOrDefault(horario.IdPrestador, 1);
        }

        return result.ToDictionary(item => item.Key, item => RoundTwo(item.Value));
    }

    private static InformeRange NormalizeRange(InformeInteligenteQuery query)
    {
        var now = DateTime.Today;
        var fechaDesde = (query.FechaDesde ?? new DateTime(now.Year, now.Month, 1)).Date;
        var fechaHasta = (query.FechaHasta ?? now).Date;

        if (fechaHasta < fechaDesde)
        {
            return InformeRange.Invalid(nameof(query.FechaHasta), "La fecha hasta no puede ser menor que la fecha desde.");
        }

        var days = (fechaHasta - fechaDesde).Days + 1;
        if (days > MaxRangeDays)
        {
            return InformeRange.Invalid(nameof(query.FechaHasta), $"El rango del informe inteligente no puede superar {MaxRangeDays} días.");
        }

        return InformeRange.Valid(fechaDesde, fechaHasta);
    }

    private static InformeRange BuildPreviousRange(InformeRange current)
    {
        var days = (current.FechaHastaInclusive - current.FechaDesde).Days + 1;
        var previousHasta = current.FechaDesde.AddDays(-1);
        var previousDesde = previousHasta.AddDays(-(days - 1));
        return InformeRange.Valid(previousDesde, previousHasta);
    }

    private static InformeInteligentePeriodoDto ToPeriodoDto(InformeRange range)
    {
        var days = (range.FechaHastaInclusive - range.FechaDesde).Days + 1;
        return new InformeInteligentePeriodoDto(
            range.FechaDesde,
            range.FechaHastaInclusive,
            days,
            $"{range.FechaDesde:yyyy-MM-dd} a {range.FechaHastaInclusive:yyyy-MM-dd}");
    }

    private static string BuildPrompt(NegocioInformeData negocio, InformeRange range)
    {
        return $"""
        Eres un analista senior de gestión para una plataforma SaaS de reservas llamada TuCita.

        Genera un informe ejecutivo en español para el negocio "{negocio.Nombre}" del rubro "{negocio.Rubro}", usando exclusivamente el objeto JSON de datos que acompaña este prompt.

        Período analizado: {range.FechaDesde:yyyy-MM-dd} a {range.FechaHastaInclusive:yyyy-MM-dd}.

        Instrucciones:
        - Escribe con tono profesional, claro y accionable.
        - No inventes datos que no estén en el JSON.
        - Si hay pocos datos, indícalo como limitación.
        - Compara contra el período anterior si el JSON incluye la sección comparacionPeriodoAnterior.
        - Destaca patrones de demanda, ocupación, cancelaciones, no asistencia, clientes nuevos/recurrentes, servicios y prestadores.
        - Entrega recomendaciones concretas para mejorar agenda, disponibilidad, recordatorios, ventas y operación.
        - Incluye una sección de indicadores principales y otra de recomendaciones priorizadas.
        """;
    }

    private static void AddDurationByHour(Dictionary<int, decimal> target, DateTime start, DateTime end)
    {
        var cursor = start;
        while (cursor < end)
        {
            var nextHour = new DateTime(cursor.Year, cursor.Month, cursor.Day, cursor.Hour, 0, 0).AddHours(1);
            var segmentEnd = nextHour < end ? nextHour : end;
            var hours = Math.Max(0, (decimal)(segmentEnd - cursor).TotalHours);
            target[cursor.Hour] = target.GetValueOrDefault(cursor.Hour) + hours;
            cursor = segmentEnd;
        }
    }

    private static IEnumerable<int> GetScheduledHours(HorarioPrestadorInformeData horario)
    {
        var startHour = horario.HoraInicio.Hour;
        var endHour = horario.HoraFin.Minute == 0 ? horario.HoraFin.Hour : horario.HoraFin.Hour + 1;

        for (var hour = startHour; hour < Math.Min(24, endHour); hour++)
        {
            yield return hour;
        }
    }

    private static IEnumerable<DateTime> EachDate(DateTime start, DateTime endInclusive)
    {
        for (var date = start.Date; date <= endInclusive.Date; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    private static bool CountsAsEstimatedIncome(CitaInformeData cita)
    {
        return !IsCancelled(cita) && !IsNoShow(cita);
    }

    private static bool IsPending(CitaInformeData cita) => NormalizeState(cita.Estado) == PendingStateName;

    private static bool IsConfirmed(CitaInformeData cita) => NormalizeState(cita.Estado) == ConfirmedStateName;

    private static bool IsAttended(CitaInformeData cita) => NormalizeState(cita.Estado) == AttendedStateName;

    private static bool IsCancelled(CitaInformeData cita) => NormalizeState(cita.Estado) == CancelledStateName;

    private static bool IsNoShow(CitaInformeData cita) => NormalizeState(cita.Estado) == NoShowStateName;

    private static decimal CalculateNoShowRate(IReadOnlyCollection<CitaInformeData> citas)
    {
        var attended = citas.Count(IsAttended);
        var noShow = citas.Count(IsNoShow);
        return Rate(noShow, attended + noShow);
    }

    private static decimal GetDurationHours(CitaInformeData cita)
    {
        return Math.Max(0, (decimal)(cita.FechaFin - cita.FechaInicio).TotalHours);
    }

    private static int GetDurationMinutes(CitaInformeData cita)
    {
        return Math.Max(0, (int)Math.Round((cita.FechaFin - cita.FechaInicio).TotalMinutes));
    }

    private static decimal Rate(decimal numerator, decimal denominator)
    {
        return denominator <= 0 ? 0 : Math.Round(numerator / denominator, 4);
    }

    private static decimal Variation(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return current == 0 ? 0 : 1;
        }

        return Math.Round((current - previous) / previous, 4);
    }

    private static decimal RoundMoney(decimal value) => Math.Round(value, 2);

    private static decimal RoundTwo(decimal value) => Math.Round(value, 2);

    private static int ToBusinessDayOfWeek(DateTime date)
    {
        return date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
    }

    private static string GetDayName(int diaSemana)
    {
        return diaSemana switch
        {
            1 => "Lunes",
            2 => "Martes",
            3 => "Miércoles",
            4 => "Jueves",
            5 => "Viernes",
            6 => "Sábado",
            7 => "Domingo",
            _ => "Sin día"
        };
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

    private sealed record NegocioInformeData(
        int IdNegocio,
        string Nombre,
        string Slug,
        string Rubro,
        bool Activo);

    private sealed record ServicioInformeData(
        int IdServicio,
        string Nombre,
        int DuracionMinutos,
        decimal Precio);

    private sealed record PrestadorInformeData(
        int IdPrestador,
        string Nombre,
        int Capacidad);

    private sealed record HorarioPrestadorInformeData(
        int IdPrestador,
        byte DiaSemana,
        TimeOnly HoraInicio,
        TimeOnly HoraFin);

    private sealed record CitaInformeData(
        int IdCita,
        string Codigo,
        int IdCliente,
        string Cliente,
        int IdServicio,
        string Servicio,
        int? IdPrestador,
        string? Prestador,
        int IdEstadoCita,
        string Estado,
        bool EsEstadoFinal,
        DateTime FechaInicio,
        DateTime FechaFin,
        DateTime FechaCreacion,
        decimal PrecioEstimado);

    private sealed record InformeRange(
        bool IsValid,
        DateTime FechaDesde,
        DateTime FechaHastaInclusive,
        DateTime FechaHastaExclusive,
        string? Field,
        string? Error)
    {
        public static InformeRange Valid(DateTime fechaDesde, DateTime fechaHastaInclusive)
        {
            return new InformeRange(true, fechaDesde, fechaHastaInclusive, fechaHastaInclusive.AddDays(1), null, null);
        }

        public static InformeRange Invalid(string field, string error)
        {
            return new InformeRange(false, default, default, default, field, error);
        }
    }
}
