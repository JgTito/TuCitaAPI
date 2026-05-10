using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TuCita.Application.Agenda;
using TuCita.Application.Common;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Agenda;

public sealed class AgendaService(ReservaFlowDbContext dbContext) : IAgendaService
{
    private const int MaxRangeDays = 62;

    public async Task<ServiceResult<MiAgendaDto>> GetMiAgendaAsync(
        CurrentUserContext currentUser,
        MiAgendaQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return ServiceResult<MiAgendaDto>.Forbidden("Debes iniciar sesión para ver tu agenda.");
        }

        var range = NormalizeRange(query);
        if (!range.IsValid)
        {
            return ServiceResult<MiAgendaDto>.Validation([
                new ValidationError(range.Field ?? string.Empty, range.Error!)
            ]);
        }

        var prestadores = await GetUserPrestadoresAsync(currentUser.UserId, query, cancellationToken);
        if (query.IdPrestador.HasValue && prestadores.Count == 0)
        {
            return ServiceResult<MiAgendaDto>.Forbidden("No tienes acceso a la agenda del prestador indicado.");
        }

        if (query.IdNegocio.HasValue && prestadores.Count == 0)
        {
            return ServiceResult<MiAgendaDto>.Forbidden("No tienes un prestador asociado al negocio indicado.");
        }

        var validationErrors = await ValidateFiltersAsync(query, prestadores, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<MiAgendaDto>.Validation(validationErrors);
        }

        var providerIds = prestadores.Select(item => item.IdPrestador).ToArray();
        var negocioIds = prestadores.Select(item => item.IdNegocio).Distinct().ToArray();

        var citas = providerIds.Length == 0
            ? []
            : await GetCitasAsync(providerIds, negocioIds, query, range, cancellationToken);
        var bloqueos = providerIds.Length == 0 || !query.IncluirBloqueos
            ? []
            : await GetBloqueosAsync(providerIds, negocioIds, query, range, cancellationToken);
        IReadOnlyCollection<MiAgendaSlotDisponibleDto> slots = [];

        var dias = BuildDays(range, citas, slots, bloqueos);
        var eventos = BuildEvents(citas, slots, bloqueos).ToArray();
        var resumen = new AgendaResumenDto(
            citas.Count,
            citas.Count(cita => !cita.EsEstadoFinal),
            citas.Count(cita => cita.EsEstadoFinal),
            slots.Count,
            bloqueos.Count,
            citas.Where(cita => !IsCancelledOrNoShow(cita.EstadoCita)).Sum(cita => cita.PrecioEstimado));

        return ServiceResult<MiAgendaDto>.Success(new MiAgendaDto(
            range.FechaDesde,
            range.FechaHasta,
            query.IntervaloMinutos,
            query.IdNegocio,
            query.IdServicio,
            query.IdPrestador,
            false,
            resumen,
            prestadores,
            dias,
            eventos));
    }

    public async Task<ServiceResult<MovimientosDisponiblesDto>> GetMovimientosDisponiblesAsync(
        CurrentUserContext currentUser,
        int idCita,
        MovimientosDisponiblesQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return ServiceResult<MovimientosDisponiblesDto>.Forbidden("Debes iniciar sesión para ver movimientos disponibles.");
        }

        var range = NormalizeRange(query);
        if (!range.IsValid)
        {
            return ServiceResult<MovimientosDisponiblesDto>.Validation([
                new ValidationError(range.Field ?? string.Empty, range.Error!)
            ]);
        }

        var cita = await dbContext.Citas
            .AsNoTracking()
            .Where(item =>
                item.IdCita == idCita &&
                item.IdPrestador.HasValue &&
                item.Prestador != null &&
                item.Prestador.UserId == currentUser.UserId &&
                item.Prestador.Activo &&
                item.Negocio.Activo)
            .Select(item => new CitaMovimientoData(
                item.IdCita,
                item.Codigo,
                item.IdNegocio,
                item.Negocio.Nombre,
                item.IdServicio,
                item.Servicio.Nombre,
                item.Servicio.DuracionMinutos,
                item.Servicio.TiempoPreparacionMinutos,
                item.IdPrestador,
                item.Prestador == null ? null : item.Prestador.Nombre,
                item.FechaInicio,
                item.FechaFin,
                item.EstadoCita.EsEstadoFinal))
            .FirstOrDefaultAsync(cancellationToken);

        if (cita is null)
        {
            return ServiceResult<MovimientosDisponiblesDto>.NotFound("La cita no existe o no pertenece a tu agenda.");
        }

        if (cita.EsEstadoFinal)
        {
            return ServiceResult<MovimientosDisponiblesDto>.Validation([
                new ValidationError(string.Empty, "No se puede mover una cita que ya está en estado final.")
            ]);
        }

        var targetPrestadores = await GetTargetPrestadoresForMoveAsync(
            currentUser.UserId,
            cita.IdNegocio,
            cita.IdServicio,
            query.IdPrestador,
            cancellationToken);

        if (query.IdPrestador.HasValue && targetPrestadores.Count == 0)
        {
            return ServiceResult<MovimientosDisponiblesDto>.Forbidden("No tienes acceso al prestador destino o no tiene asignado el servicio de la cita.");
        }

        var slots = await BuildMoveSlotsAsync(cita, targetPrestadores, query, range, cancellationToken);

        return ServiceResult<MovimientosDisponiblesDto>.Success(new MovimientosDisponiblesDto(
            cita.IdCita,
            cita.Codigo,
            cita.IdNegocio,
            cita.Negocio,
            cita.IdServicio,
            cita.Servicio,
            cita.IdPrestadorActual,
            cita.PrestadorActual,
            cita.FechaInicioActual,
            cita.FechaFinActual,
            slots));
    }

    private async Task<IReadOnlyCollection<MiAgendaPrestadorDto>> GetUserPrestadoresAsync(
        string userId,
        MiAgendaQuery query,
        CancellationToken cancellationToken)
    {
        var prestadoresQuery = dbContext.Prestadores
            .AsNoTracking()
            .Where(prestador =>
                prestador.UserId == userId &&
                prestador.Activo &&
                prestador.Negocio.Activo);

        if (query.IdNegocio.HasValue)
        {
            prestadoresQuery = prestadoresQuery.Where(prestador => prestador.IdNegocio == query.IdNegocio.Value);
        }

        if (query.IdPrestador.HasValue)
        {
            prestadoresQuery = prestadoresQuery.Where(prestador => prestador.IdPrestador == query.IdPrestador.Value);
        }

        return await prestadoresQuery
            .OrderBy(prestador => prestador.Negocio.Nombre)
            .ThenBy(prestador => prestador.Nombre)
            .Select(prestador => new MiAgendaPrestadorDto(
                prestador.IdNegocio,
                prestador.Negocio.Nombre,
                prestador.IdPrestador,
                prestador.Nombre,
                prestador.IdTipoPrestador,
                prestador.TipoPrestador.Nombre))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<MiAgendaPrestadorDto>> GetTargetPrestadoresForMoveAsync(
        string userId,
        int idNegocio,
        int idServicio,
        int? idPrestador,
        CancellationToken cancellationToken)
    {
        var prestadoresQuery = dbContext.Prestadores
            .AsNoTracking()
            .Where(prestador =>
                prestador.UserId == userId &&
                prestador.IdNegocio == idNegocio &&
                prestador.Activo &&
                prestador.Negocio.Activo &&
                prestador.PrestadorServicios.Any(relacion =>
                    relacion.IdNegocio == idNegocio &&
                    relacion.IdServicio == idServicio &&
                    relacion.Activo &&
                    relacion.Servicio.Activo));

        if (idPrestador.HasValue)
        {
            prestadoresQuery = prestadoresQuery.Where(prestador => prestador.IdPrestador == idPrestador.Value);
        }

        return await prestadoresQuery
            .OrderBy(prestador => prestador.Nombre)
            .Select(prestador => new MiAgendaPrestadorDto(
                prestador.IdNegocio,
                prestador.Negocio.Nombre,
                prestador.IdPrestador,
                prestador.Nombre,
                prestador.IdTipoPrestador,
                prestador.TipoPrestador.Nombre))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<List<ValidationError>> ValidateFiltersAsync(
        MiAgendaQuery query,
        IReadOnlyCollection<MiAgendaPrestadorDto> prestadores,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (query.IdEstadoCita.HasValue)
        {
            var estadoExists = await dbContext.EstadosCita.AnyAsync(
                estado => estado.IdEstadoCita == query.IdEstadoCita.Value && estado.Activo,
                cancellationToken);

            if (!estadoExists)
            {
                errors.Add(new ValidationError(nameof(MiAgendaQuery.IdEstadoCita), "El estado de cita indicado no existe o no está activo."));
            }
        }

        if (query.IdServicio.HasValue && prestadores.Count > 0)
        {
            var providerIds = prestadores.Select(item => item.IdPrestador).ToArray();
            var servicioAsignado = await dbContext.PrestadorServicios.AnyAsync(
                relacion =>
                    providerIds.Contains(relacion.IdPrestador) &&
                    relacion.IdServicio == query.IdServicio.Value &&
                    relacion.Activo &&
                    relacion.Servicio.Activo,
                cancellationToken);

            if (!servicioAsignado)
            {
                errors.Add(new ValidationError(nameof(MiAgendaQuery.IdServicio), "El servicio indicado no está asignado a tu prestador o no está activo."));
            }
        }

        return errors;
    }

    private async Task<IReadOnlyCollection<MiAgendaCitaDto>> GetCitasAsync(
        IReadOnlyCollection<int> providerIds,
        IReadOnlyCollection<int> negocioIds,
        MiAgendaQuery query,
        AgendaRange range,
        CancellationToken cancellationToken)
    {
        var citasQuery = dbContext.Citas
            .AsNoTracking()
            .Where(cita =>
                cita.IdPrestador.HasValue &&
                providerIds.Contains(cita.IdPrestador.Value) &&
                negocioIds.Contains(cita.IdNegocio) &&
                cita.FechaInicio < range.FechaHastaExclusive &&
                cita.FechaFin > range.FechaDesdeDateTime);

        if (query.IdServicio.HasValue)
        {
            citasQuery = citasQuery.Where(cita => cita.IdServicio == query.IdServicio.Value);
        }

        if (query.IdEstadoCita.HasValue)
        {
            citasQuery = citasQuery.Where(cita => cita.IdEstadoCita == query.IdEstadoCita.Value);
        }

        if (!query.IncluirEstadosFinales)
        {
            citasQuery = citasQuery.Where(cita => !cita.EstadoCita.EsEstadoFinal);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            citasQuery = citasQuery.Where(cita =>
                cita.Codigo.Contains(search) ||
                cita.Cliente.Nombre.Contains(search) ||
                cita.Servicio.Nombre.Contains(search) ||
                (cita.Prestador != null && cita.Prestador.Nombre.Contains(search)) ||
                cita.Negocio.Nombre.Contains(search));
        }

        return await citasQuery
            .OrderBy(cita => cita.FechaInicio)
            .ThenBy(cita => cita.Negocio.Nombre)
            .ThenBy(cita => cita.Prestador == null ? string.Empty : cita.Prestador.Nombre)
            .Select(cita => new MiAgendaCitaDto(
                cita.IdNegocio,
                cita.Negocio.Nombre,
                cita.IdCita,
                cita.Codigo,
                cita.IdCliente,
                cita.Cliente.Nombre,
                cita.IdServicio,
                cita.Servicio.Nombre,
                cita.IdPrestador!.Value,
                cita.Prestador!.Nombre,
                cita.IdEstadoCita,
                cita.EstadoCita.Nombre,
                cita.EstadoCita.EsEstadoFinal,
                cita.FechaInicio,
                cita.FechaFin,
                cita.PrecioEstimado,
                cita.ComentarioCliente,
                cita.NotaInterna))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<MiAgendaBloqueoDto>> GetBloqueosAsync(
        IReadOnlyCollection<int> providerIds,
        IReadOnlyCollection<int> negocioIds,
        MiAgendaQuery query,
        AgendaRange range,
        CancellationToken cancellationToken)
    {
        var bloqueosQuery = dbContext.BloqueosHorario
            .AsNoTracking()
            .Where(bloqueo =>
                negocioIds.Contains(bloqueo.IdNegocio) &&
                bloqueo.Activo &&
                (!bloqueo.IdPrestador.HasValue || providerIds.Contains(bloqueo.IdPrestador.Value)) &&
                bloqueo.FechaInicio < range.FechaHastaExclusive &&
                bloqueo.FechaFin > range.FechaDesdeDateTime);

        if (query.IdPrestador.HasValue)
        {
            bloqueosQuery = bloqueosQuery.Where(bloqueo =>
                !bloqueo.IdPrestador.HasValue ||
                bloqueo.IdPrestador == query.IdPrestador.Value);
        }

        return await bloqueosQuery
            .OrderBy(bloqueo => bloqueo.FechaInicio)
            .ThenBy(bloqueo => bloqueo.Negocio.Nombre)
            .Select(bloqueo => new MiAgendaBloqueoDto(
                bloqueo.IdNegocio,
                bloqueo.Negocio.Nombre,
                bloqueo.IdBloqueoHorario,
                bloqueo.IdPrestador,
                bloqueo.Prestador == null ? null : bloqueo.Prestador.Nombre,
                bloqueo.FechaInicio,
                bloqueo.FechaFin,
                bloqueo.Motivo))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<MovimientoDisponibleSlotDto>> BuildMoveSlotsAsync(
        CitaMovimientoData cita,
        IReadOnlyCollection<MiAgendaPrestadorDto> prestadores,
        MovimientosDisponiblesQuery query,
        AgendaRange range,
        CancellationToken cancellationToken)
    {
        if (prestadores.Count == 0)
        {
            return [];
        }

        var providerIds = prestadores.Select(item => item.IdPrestador).ToArray();

        var horariosNegocio = await dbContext.HorariosNegocio
            .AsNoTracking()
            .Where(horario => horario.IdNegocio == cita.IdNegocio && horario.Activo)
            .Select(horario => new ScheduleData(horario.IdNegocio, null, horario.DiaSemana, horario.HoraInicio, horario.HoraFin))
            .ToArrayAsync(cancellationToken);

        var horariosPrestador = await dbContext.HorariosPrestador
            .AsNoTracking()
            .Where(horario =>
                horario.IdNegocio == cita.IdNegocio &&
                providerIds.Contains(horario.IdPrestador) &&
                horario.Activo)
            .Select(horario => new ScheduleData(horario.IdNegocio, horario.IdPrestador, horario.DiaSemana, horario.HoraInicio, horario.HoraFin))
            .ToArrayAsync(cancellationToken);

        var bloqueos = await dbContext.BloqueosHorario
            .AsNoTracking()
            .Where(bloqueo =>
                bloqueo.IdNegocio == cita.IdNegocio &&
                bloqueo.Activo &&
                bloqueo.FechaInicio < range.FechaHastaExclusive &&
                bloqueo.FechaFin > range.FechaDesdeDateTime)
            .Select(bloqueo => new BusyRangeData(bloqueo.IdNegocio, bloqueo.IdPrestador, bloqueo.FechaInicio, bloqueo.FechaFin))
            .ToArrayAsync(cancellationToken);

        var regla = await dbContext.ReglasReserva
            .AsNoTracking()
            .Where(item => item.IdNegocio == cita.IdNegocio)
            .Select(item => new ReglaAgendaData(
                item.IdNegocio,
                item.MinHorasAnticipacion,
                item.MaxDiasAdelanto,
                item.PermiteSobreturnos))
            .FirstOrDefaultAsync(cancellationToken)
            ?? new ReglaAgendaData(cita.IdNegocio, 0, MaxRangeDays, false);

        var citasOcupadas = regla.PermiteSobreturnos
            ? Array.Empty<BusyRangeData>()
            : await dbContext.Citas
                .AsNoTracking()
                .Where(item =>
                    item.IdNegocio == cita.IdNegocio &&
                    item.IdCita != cita.IdCita &&
                    item.IdPrestador.HasValue &&
                    providerIds.Contains(item.IdPrestador.Value) &&
                    !item.EstadoCita.EsEstadoFinal &&
                    item.FechaInicio < range.FechaHastaExclusive &&
                    item.FechaFin.AddMinutes(item.Servicio.TiempoPreparacionMinutos) > range.FechaDesdeDateTime)
                .Select(item => new BusyRangeData(
                    item.IdNegocio,
                    item.IdPrestador,
                    item.FechaInicio,
                    item.FechaFin.AddMinutes(item.Servicio.TiempoPreparacionMinutos)))
                .ToArrayAsync(cancellationToken);

        var slots = new List<MovimientoDisponibleSlotDto>();
        var minFechaInicio = DateTime.Now.AddHours(regla.MinHorasAnticipacion);
        var maxFechaInicioExclusive = DateTime.Now.Date.AddDays(regla.MaxDiasAdelanto + 1);
        var duracionTotal = cita.DuracionMinutos + cita.TiempoPreparacionMinutos;

        foreach (var date in EachDay(range.FechaDesde, range.FechaHasta))
        {
            var diaSemana = GetDiaSemana(date);
            var negocioRanges = MergeRanges(horariosNegocio
                .Where(horario => horario.DiaSemana == diaSemana)
                .Select(horario => new TimeRange(date.ToDateTime(horario.HoraInicio), date.ToDateTime(horario.HoraFin))));

            foreach (var prestador in prestadores)
            {
                var providerRanges = MergeRanges(horariosPrestador
                    .Where(horario => horario.IdPrestador == prestador.IdPrestador && horario.DiaSemana == diaSemana)
                    .Select(horario => new TimeRange(date.ToDateTime(horario.HoraInicio), date.ToDateTime(horario.HoraFin))));
                var baseRanges = Intersect(negocioRanges, providerRanges);
                var busyRanges = MergeRanges(bloqueos
                    .Where(item => !item.IdPrestador.HasValue || item.IdPrestador == prestador.IdPrestador)
                    .Concat(citasOcupadas.Where(item => item.IdPrestador == prestador.IdPrestador))
                    .Select(item => new TimeRange(item.FechaInicio, item.FechaFin)));

                foreach (var baseRange in baseRanges)
                {
                    var cursor = baseRange.Inicio < minFechaInicio
                        ? RoundUp(minFechaInicio, query.IntervaloMinutos)
                        : baseRange.Inicio;
                    var lastStart = baseRange.Fin.AddMinutes(-duracionTotal);

                    while (cursor <= lastStart && cursor < maxFechaInicioExclusive)
                    {
                        var slotFinConPreparacion = cursor.AddMinutes(duracionTotal);
                        if (!Overlaps(cursor, slotFinConPreparacion, busyRanges))
                        {
                            slots.Add(new MovimientoDisponibleSlotDto(
                                cursor,
                                cursor.AddMinutes(cita.DuracionMinutos),
                                prestador.IdPrestador,
                                prestador.Prestador));
                        }

                        cursor = cursor.AddMinutes(query.IntervaloMinutos);
                    }
                }
            }
        }

        return slots
            .GroupBy(slot => new { slot.FechaInicio, slot.FechaFin, slot.IdPrestador })
            .Select(group => group.First())
            .OrderBy(slot => slot.FechaInicio)
            .ThenBy(slot => slot.Prestador)
            .ToArray();
    }

    private async Task<IReadOnlyCollection<MiAgendaSlotDisponibleDto>> BuildSlotsAsync(
        IReadOnlyCollection<MiAgendaPrestadorDto> prestadores,
        MiAgendaQuery query,
        AgendaRange range,
        CancellationToken cancellationToken)
    {
        var idServicio = query.IdServicio!.Value;
        var providerIds = prestadores.Select(item => item.IdPrestador).ToArray();
        var negocioIds = prestadores.Select(item => item.IdNegocio).Distinct().ToArray();
        var providerLookup = prestadores.ToDictionary(item => item.IdPrestador);

        var servicios = await dbContext.Servicios
            .AsNoTracking()
            .Where(servicio =>
                servicio.IdServicio == idServicio &&
                negocioIds.Contains(servicio.IdNegocio) &&
                servicio.Activo)
            .Select(servicio => new ServicioAgendaData(
                servicio.IdNegocio,
                servicio.IdServicio,
                servicio.Nombre,
                servicio.DuracionMinutos,
                servicio.TiempoPreparacionMinutos))
            .ToArrayAsync(cancellationToken);

        if (servicios.Length == 0)
        {
            return [];
        }

        var assignedProviderIds = await dbContext.PrestadorServicios
            .AsNoTracking()
            .Where(relacion =>
                relacion.IdServicio == idServicio &&
                providerIds.Contains(relacion.IdPrestador) &&
                relacion.Activo &&
                relacion.Servicio.Activo)
            .Select(relacion => relacion.IdPrestador)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var horariosNegocio = await dbContext.HorariosNegocio
            .AsNoTracking()
            .Where(horario => negocioIds.Contains(horario.IdNegocio) && horario.Activo)
            .Select(horario => new ScheduleData(horario.IdNegocio, null, horario.DiaSemana, horario.HoraInicio, horario.HoraFin))
            .ToArrayAsync(cancellationToken);

        var horariosPrestador = await dbContext.HorariosPrestador
            .AsNoTracking()
            .Where(horario =>
                negocioIds.Contains(horario.IdNegocio) &&
                assignedProviderIds.Contains(horario.IdPrestador) &&
                horario.Activo)
            .Select(horario => new ScheduleData(horario.IdNegocio, horario.IdPrestador, horario.DiaSemana, horario.HoraInicio, horario.HoraFin))
            .ToArrayAsync(cancellationToken);

        var bloqueos = await dbContext.BloqueosHorario
            .AsNoTracking()
            .Where(bloqueo =>
                negocioIds.Contains(bloqueo.IdNegocio) &&
                bloqueo.Activo &&
                bloqueo.FechaInicio < range.FechaHastaExclusive &&
                bloqueo.FechaFin > range.FechaDesdeDateTime)
            .Select(bloqueo => new BusyRangeData(bloqueo.IdNegocio, bloqueo.IdPrestador, bloqueo.FechaInicio, bloqueo.FechaFin))
            .ToArrayAsync(cancellationToken);

        var reglas = await dbContext.ReglasReserva
            .AsNoTracking()
            .Where(regla => negocioIds.Contains(regla.IdNegocio))
            .Select(regla => new ReglaAgendaData(regla.IdNegocio, regla.MinHorasAnticipacion, regla.MaxDiasAdelanto, regla.PermiteSobreturnos))
            .ToDictionaryAsync(regla => regla.IdNegocio, cancellationToken);

        var citasOcupadas = await dbContext.Citas
            .AsNoTracking()
            .Where(cita =>
                cita.IdPrestador.HasValue &&
                assignedProviderIds.Contains(cita.IdPrestador.Value) &&
                !cita.EstadoCita.EsEstadoFinal &&
                cita.FechaInicio < range.FechaHastaExclusive &&
                cita.FechaFin.AddMinutes(cita.Servicio.TiempoPreparacionMinutos) > range.FechaDesdeDateTime)
            .Select(cita => new BusyRangeData(
                cita.IdNegocio,
                cita.IdPrestador,
                cita.FechaInicio,
                cita.FechaFin.AddMinutes(cita.Servicio.TiempoPreparacionMinutos)))
            .ToArrayAsync(cancellationToken);

        var slots = new List<MiAgendaSlotDisponibleDto>();

        foreach (var servicio in servicios)
        {
            var servicioPrestadores = assignedProviderIds
                .Select(idPrestador => providerLookup[idPrestador])
                .Where(prestador => prestador.IdNegocio == servicio.IdNegocio)
                .ToArray();
            var regla = reglas.TryGetValue(servicio.IdNegocio, out var value)
                ? value
                : new ReglaAgendaData(servicio.IdNegocio, 0, MaxRangeDays, false);
            var minFechaInicio = DateTime.Now.AddHours(regla.MinHorasAnticipacion);
            var duracionTotal = servicio.DuracionMinutos + servicio.TiempoPreparacionMinutos;

            foreach (var date in EachDay(range.FechaDesde, range.FechaHasta))
            {
                var diaSemana = GetDiaSemana(date);
                var negocioRanges = MergeRanges(horariosNegocio
                    .Where(horario => horario.IdNegocio == servicio.IdNegocio && horario.DiaSemana == diaSemana)
                    .Select(horario => new TimeRange(date.ToDateTime(horario.HoraInicio), date.ToDateTime(horario.HoraFin))));

                foreach (var prestador in servicioPrestadores)
                {
                    var providerRanges = MergeRanges(horariosPrestador
                        .Where(horario =>
                            horario.IdNegocio == servicio.IdNegocio &&
                            horario.IdPrestador == prestador.IdPrestador &&
                            horario.DiaSemana == diaSemana)
                        .Select(horario => new TimeRange(date.ToDateTime(horario.HoraInicio), date.ToDateTime(horario.HoraFin))));
                    var baseRanges = Intersect(negocioRanges, providerRanges);
                    var busyRanges = MergeRanges(bloqueos
                        .Where(item =>
                            item.IdNegocio == servicio.IdNegocio &&
                            (!item.IdPrestador.HasValue || item.IdPrestador == prestador.IdPrestador))
                        .Concat(regla.PermiteSobreturnos
                            ? []
                            : citasOcupadas.Where(item =>
                                item.IdNegocio == servicio.IdNegocio &&
                                item.IdPrestador == prestador.IdPrestador))
                        .Select(item => new TimeRange(item.FechaInicio, item.FechaFin)));

                    foreach (var baseRange in baseRanges)
                    {
                        var cursor = baseRange.Inicio < minFechaInicio
                            ? RoundUp(minFechaInicio, query.IntervaloMinutos)
                            : baseRange.Inicio;
                        var lastStart = baseRange.Fin.AddMinutes(-duracionTotal);

                        while (cursor <= lastStart)
                        {
                            var slotFin = cursor.AddMinutes(duracionTotal);
                            if (!Overlaps(cursor, slotFin, busyRanges))
                            {
                                slots.Add(new MiAgendaSlotDisponibleDto(
                                    servicio.IdNegocio,
                                    prestador.Negocio,
                                    cursor,
                                    cursor.AddMinutes(servicio.DuracionMinutos),
                                    prestador.IdPrestador,
                                    prestador.Prestador,
                                    servicio.IdServicio,
                                    servicio.Nombre));
                            }

                            cursor = cursor.AddMinutes(query.IntervaloMinutos);
                        }
                    }
                }
            }
        }

        return slots
            .GroupBy(slot => new { slot.IdNegocio, slot.IdPrestador, slot.IdServicio, slot.FechaInicio, slot.FechaFin })
            .Select(group => group.First())
            .OrderBy(slot => slot.FechaInicio)
            .ThenBy(slot => slot.Negocio)
            .ThenBy(slot => slot.Prestador)
            .ToArray();
    }

    private static IReadOnlyCollection<MiAgendaDiaDto> BuildDays(
        AgendaRange range,
        IReadOnlyCollection<MiAgendaCitaDto> citas,
        IReadOnlyCollection<MiAgendaSlotDisponibleDto> slots,
        IReadOnlyCollection<MiAgendaBloqueoDto> bloqueos)
    {
        var citasByDate = citas
            .GroupBy(cita => DateOnly.FromDateTime(cita.FechaInicio))
            .ToDictionary(group => group.Key, group => group.ToArray());
        var slotsByDate = slots
            .GroupBy(slot => DateOnly.FromDateTime(slot.FechaInicio))
            .ToDictionary(group => group.Key, group => group.ToArray());
        var bloqueosByDate = bloqueos
            .GroupBy(bloqueo => DateOnly.FromDateTime(bloqueo.FechaInicio))
            .ToDictionary(group => group.Key, group => group.ToArray());

        return EachDay(range.FechaDesde, range.FechaHasta)
            .Select(date =>
            {
                var dayCitas = citasByDate.TryGetValue(date, out var citasValue) ? citasValue : [];
                var daySlots = slotsByDate.TryGetValue(date, out var slotsValue) ? slotsValue : [];
                var dayBloqueos = bloqueosByDate.TryGetValue(date, out var bloqueosValue) ? bloqueosValue : [];

                return new MiAgendaDiaDto(
                    date,
                    new AgendaDiaResumenDto(
                        dayCitas.Length,
                        dayCitas.Count(cita => !cita.EsEstadoFinal),
                        dayCitas.Count(cita => cita.EsEstadoFinal),
                        daySlots.Length,
                        dayBloqueos.Length),
                    dayCitas,
                    daySlots,
                    dayBloqueos);
            })
            .ToArray();
    }

    private static IEnumerable<MiAgendaEventoDto> BuildEvents(
        IReadOnlyCollection<MiAgendaCitaDto> citas,
        IReadOnlyCollection<MiAgendaSlotDisponibleDto> slots,
        IReadOnlyCollection<MiAgendaBloqueoDto> bloqueos)
    {
        foreach (var cita in citas)
        {
            yield return new MiAgendaEventoDto(
                "Cita",
                cita.IdCita,
                $"{cita.Codigo} - {cita.Cliente}",
                cita.IdNegocio,
                cita.Negocio,
                cita.FechaInicio,
                cita.FechaFin,
                cita.IdCita,
                cita.IdCliente,
                cita.Cliente,
                cita.IdServicio,
                cita.Servicio,
                cita.IdPrestador,
                cita.Prestador,
                cita.EstadoCita,
                GetCitaColor(cita.EstadoCita, cita.EsEstadoFinal));
        }

        foreach (var slot in slots)
        {
            yield return new MiAgendaEventoDto(
                "Disponible",
                null,
                "Hora disponible",
                slot.IdNegocio,
                slot.Negocio,
                slot.FechaInicio,
                slot.FechaFin,
                null,
                null,
                null,
                slot.IdServicio,
                slot.Servicio,
                slot.IdPrestador,
                slot.Prestador,
                null,
                "#14B8A6");
        }

        foreach (var bloqueo in bloqueos)
        {
            yield return new MiAgendaEventoDto(
                "Bloqueo",
                bloqueo.IdBloqueoHorario,
                string.IsNullOrWhiteSpace(bloqueo.Motivo) ? "Bloqueo horario" : bloqueo.Motivo,
                bloqueo.IdNegocio,
                bloqueo.Negocio,
                bloqueo.FechaInicio,
                bloqueo.FechaFin,
                null,
                null,
                null,
                null,
                null,
                bloqueo.IdPrestador,
                bloqueo.Prestador,
                "Bloqueado",
                "#DC2626");
        }
    }

    private static AgendaRange NormalizeRange(MiAgendaQuery query)
    {
        if (!query.FechaDesde.HasValue)
        {
            return AgendaRange.Invalid(nameof(query.FechaDesde), "La fecha desde es obligatoria.");
        }

        var fechaDesde = query.FechaDesde.Value;
        var fechaHasta = query.FechaHasta ?? fechaDesde;

        if (fechaHasta < fechaDesde)
        {
            return AgendaRange.Invalid(nameof(query.FechaHasta), "La fecha hasta debe ser mayor o igual a la fecha desde.");
        }

        if (fechaDesde.DayNumber + MaxRangeDays - 1 < fechaHasta.DayNumber)
        {
            return AgendaRange.Invalid(nameof(query.FechaHasta), $"El rango de agenda no puede superar {MaxRangeDays} días.");
        }

        return AgendaRange.Valid(fechaDesde, fechaHasta);
    }

    private static AgendaRange NormalizeRange(MovimientosDisponiblesQuery query)
    {
        if (!query.FechaDesde.HasValue)
        {
            return AgendaRange.Invalid(nameof(query.FechaDesde), "La fecha desde es obligatoria.");
        }

        var fechaDesde = query.FechaDesde.Value;
        var fechaHasta = query.FechaHasta ?? fechaDesde;

        if (fechaHasta < fechaDesde)
        {
            return AgendaRange.Invalid(nameof(query.FechaHasta), "La fecha hasta debe ser mayor o igual a la fecha desde.");
        }

        if (fechaDesde.DayNumber + MaxRangeDays - 1 < fechaHasta.DayNumber)
        {
            return AgendaRange.Invalid(nameof(query.FechaHasta), $"El rango de movimientos disponibles no puede superar {MaxRangeDays} días.");
        }

        return AgendaRange.Valid(fechaDesde, fechaHasta);
    }

    private static IReadOnlyCollection<TimeRange> Intersect(
        IReadOnlyCollection<TimeRange> first,
        IReadOnlyCollection<TimeRange> second)
    {
        var intersections = new List<TimeRange>();

        foreach (var left in first)
        {
            foreach (var right in second)
            {
                var inicio = left.Inicio > right.Inicio ? left.Inicio : right.Inicio;
                var fin = left.Fin < right.Fin ? left.Fin : right.Fin;

                if (fin > inicio)
                {
                    intersections.Add(new TimeRange(inicio, fin));
                }
            }
        }

        return MergeRanges(intersections);
    }

    private static IReadOnlyCollection<TimeRange> MergeRanges(IEnumerable<TimeRange> ranges)
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

        var result = new List<TimeRange>();
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

    private static IEnumerable<DateOnly> EachDay(DateOnly fechaDesde, DateOnly fechaHasta)
    {
        for (var date = fechaDesde; date <= fechaHasta; date = date.AddDays(1))
        {
            yield return date;
        }
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

    private static bool Overlaps(DateTime inicio, DateTime fin, IReadOnlyCollection<TimeRange> ranges)
    {
        return ranges.Any(range => range.Inicio < fin && range.Fin > inicio);
    }

    private static DateTime RoundUp(DateTime value, int intervalMinutes)
    {
        var intervalTicks = TimeSpan.FromMinutes(intervalMinutes).Ticks;
        var roundedTicks = ((value.Ticks + intervalTicks - 1) / intervalTicks) * intervalTicks;

        return new DateTime(roundedTicks, value.Kind);
    }

    private static string GetCitaColor(string estado, bool esEstadoFinal)
    {
        var normalized = NormalizeState(estado);
        return normalized switch
        {
            "PENDIENTE" => "#F59E0B",
            "CONFIRMADA" or "REAGENDADA" or "PENDIENTE DE PAGO" => "#2563EB",
            "CANCELADA" => "#EF4444",
            "ATENDIDA" => "#16A34A",
            "NO ASISTIO" => "#7C3AED",
            _ => esEstadoFinal ? "#64748B" : "#0F766E"
        };
    }

    private static bool IsCancelledOrNoShow(string estado)
    {
        var normalized = NormalizeState(estado);
        return normalized is "CANCELADA" or "NO ASISTIO";
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

    private sealed record ServicioAgendaData(
        int IdNegocio,
        int IdServicio,
        string Nombre,
        int DuracionMinutos,
        int TiempoPreparacionMinutos);

    private sealed record CitaMovimientoData(
        int IdCita,
        string Codigo,
        int IdNegocio,
        string Negocio,
        int IdServicio,
        string Servicio,
        int DuracionMinutos,
        int TiempoPreparacionMinutos,
        int? IdPrestadorActual,
        string? PrestadorActual,
        DateTime FechaInicioActual,
        DateTime FechaFinActual,
        bool EsEstadoFinal);

    private sealed record ScheduleData(
        int IdNegocio,
        int? IdPrestador,
        byte DiaSemana,
        TimeOnly HoraInicio,
        TimeOnly HoraFin);

    private sealed record BusyRangeData(
        int IdNegocio,
        int? IdPrestador,
        DateTime FechaInicio,
        DateTime FechaFin);

    private sealed record ReglaAgendaData(
        int IdNegocio,
        int MinHorasAnticipacion,
        int MaxDiasAdelanto,
        bool PermiteSobreturnos);

    private sealed record TimeRange(DateTime Inicio, DateTime Fin);

    private sealed record AgendaRange(
        bool IsValid,
        DateOnly FechaDesde,
        DateOnly FechaHasta,
        DateTime FechaDesdeDateTime,
        DateTime FechaHastaExclusive,
        string? Field,
        string? Error)
    {
        public static AgendaRange Valid(DateOnly fechaDesde, DateOnly fechaHasta)
        {
            return new AgendaRange(
                true,
                fechaDesde,
                fechaHasta,
                fechaDesde.ToDateTime(TimeOnly.MinValue),
                fechaHasta.AddDays(1).ToDateTime(TimeOnly.MinValue),
                null,
                null);
        }

        public static AgendaRange Invalid(string field, string error)
        {
            return new AgendaRange(false, default, default, default, default, field, error);
        }
    }
}
