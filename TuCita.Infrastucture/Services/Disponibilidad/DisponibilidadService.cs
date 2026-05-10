using Microsoft.EntityFrameworkCore;
using TuCita.Application.Common;
using TuCita.Application.Disponibilidad;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.Disponibilidad;

public sealed class DisponibilidadService(ReservaFlowDbContext dbContext) : IDisponibilidadService
{
    private const int DefaultMinHorasAnticipacion = 2;
    private const int DefaultMaxDiasAdelanto = 30;

    public async Task<ServiceResult<DisponibilidadDto>> GetDisponibilidadAsync(
        int idNegocio,
        DisponibilidadQuery query,
        CancellationToken cancellationToken)
    {
        return await GetDisponibilidadAsync(idNegocio, query, null, cancellationToken);
    }

    public async Task<ServiceResult<DisponibilidadDto>> GetDisponibilidadAsync(
        int idNegocio,
        DisponibilidadQuery query,
        int? idCitaExcluir,
        CancellationToken cancellationToken)
    {
        var negocioExists = await dbContext.Negocios
            .AsNoTracking()
            .AnyAsync(negocio => negocio.IdNegocio == idNegocio && negocio.Activo, cancellationToken);

        if (!negocioExists)
        {
            return ServiceResult<DisponibilidadDto>.NotFound("El negocio no existe o no está activo.");
        }

        var servicio = await dbContext.Servicios
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.IdNegocio == idNegocio && item.IdServicio == query.IdServicio && item.Activo,
                cancellationToken);

        if (servicio is null)
        {
            return ServiceResult<DisponibilidadDto>.Validation([
                new ValidationError(nameof(DisponibilidadQuery.IdServicio), "El servicio indicado no existe o no está activo para este negocio.")
            ]);
        }

        var validationErrors = await ValidateQueryAsync(idNegocio, servicio, query, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<DisponibilidadDto>.Validation(validationErrors);
        }

        var prestadores = await GetPrestadoresAsync(idNegocio, servicio, query.IdPrestador, cancellationToken);
        var slots = new List<DisponibilidadSlotDto>();
        var diaSemana = GetDiaSemana(query.Fecha);
        var fechaInicioDia = query.Fecha.ToDateTime(TimeOnly.MinValue);
        var fechaFinDia = query.Fecha.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var regla = await dbContext.ReglasReserva
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdNegocio == idNegocio, cancellationToken);
        var permiteSobreturnos = regla?.PermiteSobreturnos == true;

        if (!servicio.RequiereProfesional && !query.IdPrestador.HasValue)
        {
            slots.AddRange(await BuildSlotsForPrestadorAsync(
                idNegocio,
                servicio,
                query,
                diaSemana,
                fechaInicioDia,
                fechaFinDia,
                null,
                permiteSobreturnos,
                idCitaExcluir,
                cancellationToken));
        }

        foreach (var prestador in prestadores)
        {
            slots.AddRange(await BuildSlotsForPrestadorAsync(
                idNegocio,
                servicio,
                query,
                diaSemana,
                fechaInicioDia,
                fechaFinDia,
                prestador,
                permiteSobreturnos,
                idCitaExcluir,
                cancellationToken));
        }

        var result = new DisponibilidadDto(
            idNegocio,
            servicio.IdServicio,
            servicio.Nombre,
            query.Fecha,
            servicio.DuracionMinutos,
            servicio.TiempoPreparacionMinutos,
            query.IntervaloMinutos,
            slots
                .GroupBy(slot => new { slot.FechaInicio, slot.FechaFin, slot.IdPrestador })
                .Select(group => group.First())
                .OrderBy(slot => slot.FechaInicio)
                .ThenBy(slot => slot.Prestador)
                .ToArray());

        return ServiceResult<DisponibilidadDto>.Success(result);
    }

    private async Task<List<ValidationError>> ValidateQueryAsync(
        int idNegocio,
        Servicio servicio,
        DisponibilidadQuery query,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (query.Fecha == default)
        {
            errors.Add(new ValidationError(nameof(DisponibilidadQuery.Fecha), "La fecha de consulta es obligatoria."));
        }

        var regla = await dbContext.ReglasReserva
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdNegocio == idNegocio, cancellationToken);
        var minHorasAnticipacion = regla?.MinHorasAnticipacion ?? DefaultMinHorasAnticipacion;
        var maxDiasAdelanto = regla?.MaxDiasAdelanto ?? DefaultMaxDiasAdelanto;
        var desdePermitido = DateTime.Now.AddHours(minHorasAnticipacion);
        var hastaPermitido = DateOnly.FromDateTime(DateTime.Now.Date.AddDays(maxDiasAdelanto));

        if (query.Fecha < DateOnly.FromDateTime(desdePermitido.Date))
        {
            errors.Add(new ValidationError(nameof(DisponibilidadQuery.Fecha), "La fecha no cumple con la anticipación mínima configurada."));
        }

        if (query.Fecha > hastaPermitido)
        {
            errors.Add(new ValidationError(nameof(DisponibilidadQuery.Fecha), "La fecha supera el máximo de días de adelanto configurado."));
        }

        if (query.IdPrestador.HasValue)
        {
            var prestador = await dbContext.Prestadores
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.IdNegocio == idNegocio &&
                        item.IdPrestador == query.IdPrestador.Value &&
                        item.Activo,
                    cancellationToken);

            if (prestador is null)
            {
                errors.Add(new ValidationError(nameof(DisponibilidadQuery.IdPrestador), "El prestador o recurso indicado no existe o no está activo para este negocio."));
            }
            else
            {
                var canServe = await dbContext.PrestadorServicios.AnyAsync(
                    relacion =>
                        relacion.IdNegocio == idNegocio &&
                        relacion.IdPrestador == query.IdPrestador.Value &&
                        relacion.IdServicio == servicio.IdServicio &&
                        relacion.Activo,
                    cancellationToken);

                if (!canServe)
                {
                    errors.Add(new ValidationError(nameof(DisponibilidadQuery.IdPrestador), "El prestador o recurso no tiene asignado el servicio indicado."));
                }
            }
        }
        else if (servicio.RequiereProfesional)
        {
            var hasPrestadores = await dbContext.PrestadorServicios.AnyAsync(
                relacion =>
                    relacion.IdNegocio == idNegocio &&
                    relacion.IdServicio == servicio.IdServicio &&
                    relacion.Activo &&
                    relacion.Prestador.Activo,
                cancellationToken);

            if (!hasPrestadores)
            {
                errors.Add(new ValidationError(nameof(DisponibilidadQuery.IdPrestador), "El servicio requiere prestador o recurso, pero no tiene prestadores activos asignados."));
            }
        }

        return errors;
    }

    private async Task<List<Prestador>> GetPrestadoresAsync(
        int idNegocio,
        Servicio servicio,
        int? idPrestador,
        CancellationToken cancellationToken)
    {
        if (idPrestador.HasValue)
        {
            return await dbContext.Prestadores
                .AsNoTracking()
                .Where(prestador =>
                    prestador.IdNegocio == idNegocio &&
                    prestador.IdPrestador == idPrestador.Value &&
                    prestador.Activo)
                .ToListAsync(cancellationToken);
        }

        return await dbContext.PrestadorServicios
            .AsNoTracking()
            .Where(relacion =>
                relacion.IdNegocio == idNegocio &&
                relacion.IdServicio == servicio.IdServicio &&
                relacion.Activo &&
                relacion.Prestador.Activo)
            .Select(relacion => relacion.Prestador)
            .OrderBy(prestador => prestador.Nombre)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<DisponibilidadSlotDto>> BuildSlotsForPrestadorAsync(
        int idNegocio,
        Servicio servicio,
        DisponibilidadQuery query,
        byte diaSemana,
        DateTime fechaInicioDia,
        DateTime fechaFinDia,
        Prestador? prestador,
        bool permiteSobreturnos,
        int? idCitaExcluir,
        CancellationToken cancellationToken)
    {
        var horariosBase = await GetHorariosBaseAsync(idNegocio, query.Fecha, diaSemana, prestador?.IdPrestador, cancellationToken);
        if (horariosBase.Count == 0)
        {
            return [];
        }

        var intervalosOcupados = await GetIntervalosOcupadosAsync(
            idNegocio,
            prestador?.IdPrestador,
            fechaInicioDia,
            fechaFinDia,
            permiteSobreturnos,
            idCitaExcluir,
            cancellationToken);
        var minFechaInicio = DateTime.Now.AddHours(await GetMinHorasAnticipacionAsync(idNegocio, cancellationToken));
        var duracionTotal = servicio.DuracionMinutos + servicio.TiempoPreparacionMinutos;
        var slots = new List<DisponibilidadSlotDto>();

        foreach (var horario in horariosBase)
        {
            var cursor = horario.Inicio < minFechaInicio ? RoundUp(minFechaInicio, query.IntervaloMinutos) : horario.Inicio;
            var lastStart = horario.Fin.AddMinutes(-duracionTotal);

            while (cursor <= lastStart)
            {
                var slotFin = cursor.AddMinutes(duracionTotal);
                if (!Overlaps(cursor, slotFin, intervalosOcupados))
                {
                    slots.Add(new DisponibilidadSlotDto(
                        cursor,
                        cursor.AddMinutes(servicio.DuracionMinutos),
                        prestador?.IdPrestador,
                        prestador?.Nombre));
                }

                cursor = cursor.AddMinutes(query.IntervaloMinutos);
            }
        }

        return slots;
    }

    private async Task<List<TimeRange>> GetHorariosBaseAsync(
        int idNegocio,
        DateOnly fecha,
        byte diaSemana,
        int? idPrestador,
        CancellationToken cancellationToken)
    {
        var horariosNegocio = await dbContext.HorariosNegocio
            .AsNoTracking()
            .Where(horario => horario.IdNegocio == idNegocio && horario.DiaSemana == diaSemana && horario.Activo)
            .ToListAsync(cancellationToken);
        var rangosNegocio = horariosNegocio
            .Select(horario => new TimeRange(
                fecha.ToDateTime(horario.HoraInicio),
                fecha.ToDateTime(horario.HoraFin)))
            .ToArray();

        if (!idPrestador.HasValue)
        {
            return MergeRanges(rangosNegocio).ToList();
        }

        var horariosPrestador = await dbContext.HorariosPrestador
            .AsNoTracking()
            .Where(horario =>
                horario.IdNegocio == idNegocio &&
                horario.IdPrestador == idPrestador.Value &&
                horario.DiaSemana == diaSemana &&
                horario.Activo)
            .ToListAsync(cancellationToken);
        var rangosPrestador = horariosPrestador
            .Select(horario => new TimeRange(
                fecha.ToDateTime(horario.HoraInicio),
                fecha.ToDateTime(horario.HoraFin)))
            .ToArray();

        return Intersect(rangosNegocio, rangosPrestador);
    }

    private async Task<List<TimeRange>> GetIntervalosOcupadosAsync(
        int idNegocio,
        int? idPrestador,
        DateTime fechaInicioDia,
        DateTime fechaFinDia,
        bool permiteSobreturnos,
        int? idCitaExcluir,
        CancellationToken cancellationToken)
    {
        var bloqueos = await dbContext.BloqueosHorario
            .AsNoTracking()
            .Where(bloqueo =>
                bloqueo.IdNegocio == idNegocio &&
                bloqueo.Activo &&
                (!bloqueo.IdPrestador.HasValue || bloqueo.IdPrestador == idPrestador) &&
                bloqueo.FechaInicio < fechaFinDia &&
                bloqueo.FechaFin > fechaInicioDia)
            .Select(bloqueo => new TimeRange(bloqueo.FechaInicio, bloqueo.FechaFin))
            .ToListAsync(cancellationToken);

        if (permiteSobreturnos)
        {
            return MergeRanges(bloqueos).ToList();
        }

        var citas = await dbContext.Citas
            .AsNoTracking()
            .Where(cita =>
                cita.IdNegocio == idNegocio &&
                cita.IdPrestador == idPrestador &&
                (!idCitaExcluir.HasValue || cita.IdCita != idCitaExcluir.Value) &&
                !cita.EstadoCita.EsEstadoFinal &&
                cita.FechaInicio < fechaFinDia &&
                cita.FechaFin.AddMinutes(cita.Servicio.TiempoPreparacionMinutos) > fechaInicioDia)
            .Select(cita => new TimeRange(
                cita.FechaInicio,
                cita.FechaFin.AddMinutes(cita.Servicio.TiempoPreparacionMinutos)))
            .ToListAsync(cancellationToken);

        bloqueos.AddRange(citas);
        return MergeRanges(bloqueos).ToList();
    }

    private async Task<int> GetMinHorasAnticipacionAsync(int idNegocio, CancellationToken cancellationToken)
    {
        var minHoras = await dbContext.ReglasReserva
            .AsNoTracking()
            .Where(regla => regla.IdNegocio == idNegocio)
            .Select(regla => (int?)regla.MinHorasAnticipacion)
            .FirstOrDefaultAsync(cancellationToken);

        return minHoras ?? DefaultMinHorasAnticipacion;
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

    private static List<TimeRange> Intersect(
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

        return MergeRanges(intersections).ToList();
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

    private sealed record TimeRange(DateTime Inicio, DateTime Fin);
}
