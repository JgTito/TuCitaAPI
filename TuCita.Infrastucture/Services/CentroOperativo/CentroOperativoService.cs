using Microsoft.EntityFrameworkCore;
using TuCita.Application.CentroOperativo;
using TuCita.Application.Common;
using TuCita.Infrastucture.Entities;
using TuCita.Infrastucture.Persistence;

namespace TuCita.Infrastucture.CentroOperativo;

public sealed class CentroOperativoService(ReservaFlowDbContext dbContext) : ICentroOperativoService
{
    private const string OwnerRoleName = "Owner";
    private const string AdminRoleName = "Admin";
    private const string EstadoCitaPendienteName = "Pendiente";
    private const string EstadoCitaReagendadaName = "Reagendada";
    private const string EstadoPagoPendienteName = "Pendiente";
    private const string EstadoNotificacionErrorName = "Error";
    private const string PrioridadCritica = "Crítica";
    private const string PrioridadAlta = "Alta";
    private const string PrioridadMedia = "Media";

    public async Task<ServiceResult<CentroOperativoDto>> GetAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CentroOperativoQuery query,
        CancellationToken cancellationToken)
    {
        var negocio = await dbContext.Negocios
            .AsNoTracking()
            .Where(item => item.IdNegocio == idNegocio)
            .Select(item => new { item.IdNegocio, item.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (negocio is null)
        {
            return ServiceResult<CentroOperativoDto>.NotFound("El negocio no existe.");
        }

        if (!await CanViewAsync(currentUser, idNegocio, cancellationToken))
        {
            return ServiceResult<CentroOperativoDto>.Forbidden("No tienes acceso para ver el centro operativo de este negocio.");
        }

        var now = DateTime.Now;
        var limit = Math.Clamp(query.LimitePorCategoria, 1, 50);
        var citasHasta = now.AddDays(query.DiasProximasCitas);
        var erroresDesde = now.AddDays(-query.DiasErroresNotificaciones);
        var cambiosDesde = now.AddDays(-query.DiasSolicitudesCambio);

        var resenasBajasQuery = dbContext.ResenasNegocio
            .AsNoTracking()
            .Where(item => item.IdNegocio == idNegocio && item.Activo && item.EsAlertaOperativa);
        var pagosPendientesQuery = dbContext.Pagos
            .AsNoTracking()
            .Where(item => item.IdNegocio == idNegocio && item.EstadoPago.Nombre == EstadoPagoPendienteName);
        var citasSinConfirmarQuery = dbContext.Citas
            .AsNoTracking()
            .Where(item =>
                item.IdNegocio == idNegocio &&
                item.EstadoCita.Nombre == EstadoCitaPendienteName &&
                item.FechaInicio >= now &&
                item.FechaInicio <= citasHasta);
        var invitacionesVencidasQuery = dbContext.InvitacionesNegocio
            .AsNoTracking()
            .Where(item =>
                item.IdNegocio == idNegocio &&
                (item.Estado == InvitacionNegocioEstados.Expirada ||
                    (item.Estado == InvitacionNegocioEstados.Pendiente && item.FechaExpiracion <= now)));
        var notificacionesErrorQuery = dbContext.Notificaciones
            .AsNoTracking()
            .Where(item =>
                item.IdNegocio == idNegocio &&
                item.EstadoNotificacion.Nombre == EstadoNotificacionErrorName &&
                item.FechaCreacion >= erroresDesde);
        var solicitudesCambioQuery = dbContext.Citas
            .AsNoTracking()
            .Where(item =>
                item.IdNegocio == idNegocio &&
                item.EstadoCita.Nombre == EstadoCitaReagendadaName &&
                (item.FechaInicio >= now || (item.FechaActualizacion ?? item.FechaCreacion) >= cambiosDesde));

        var resenasBajasCount = await resenasBajasQuery.CountAsync(cancellationToken);
        var pagosPendientesCount = await pagosPendientesQuery.CountAsync(cancellationToken);
        var citasSinConfirmarCount = await citasSinConfirmarQuery.CountAsync(cancellationToken);
        var invitacionesVencidasCount = await invitacionesVencidasQuery.CountAsync(cancellationToken);
        var notificacionesErrorCount = await notificacionesErrorQuery.CountAsync(cancellationToken);
        var solicitudesCambioCount = await solicitudesCambioQuery.CountAsync(cancellationToken);

        var resenasBajas = await GetResenasBajasAsync(resenasBajasQuery, limit, cancellationToken);
        var pagosPendientes = await GetPagosPendientesAsync(pagosPendientesQuery, now, limit, cancellationToken);
        var citasSinConfirmar = await GetCitasSinConfirmarAsync(citasSinConfirmarQuery, now, limit, cancellationToken);
        var invitacionesVencidas = await GetInvitacionesVencidasAsync(invitacionesVencidasQuery, now, limit, cancellationToken);
        var notificacionesError = await GetNotificacionesErrorAsync(notificacionesErrorQuery, limit, cancellationToken);
        var solicitudesCambio = await GetSolicitudesCambioAsync(solicitudesCambioQuery, now, limit, cancellationToken);

        var totalAcciones =
            resenasBajasCount +
            pagosPendientesCount +
            citasSinConfirmarCount +
            invitacionesVencidasCount +
            notificacionesErrorCount +
            solicitudesCambioCount;

        var result = new CentroOperativoDto(
            negocio.IdNegocio,
            negocio.Nombre,
            now,
            new CentroOperativoResumenDto(
                totalAcciones,
                resenasBajasCount,
                pagosPendientesCount,
                citasSinConfirmarCount,
                invitacionesVencidasCount,
                notificacionesErrorCount,
                solicitudesCambioCount,
                totalAcciones > 0),
            resenasBajas,
            pagosPendientes,
            citasSinConfirmar,
            invitacionesVencidas,
            notificacionesError,
            solicitudesCambio);

        return ServiceResult<CentroOperativoDto>.Success(result);
    }

    private async Task<IReadOnlyCollection<CentroOperativoResenaBajaDto>> GetResenasBajasAsync(
        IQueryable<ResenaNegocio> query,
        int limit,
        CancellationToken cancellationToken)
    {
        var items = await query
            .OrderBy(item => item.Puntuacion)
            .ThenByDescending(item => item.FechaAlertaOperativa ?? item.FechaCreacion)
            .Take(limit)
            .Select(item => new
            {
                item.IdResenaNegocio,
                item.IdCita,
                CodigoCita = item.Cita.Codigo,
                Cliente = item.ClienteNombreSnapshot,
                Servicio = item.ServicioNombreSnapshot,
                Prestador = item.PrestadorNombreSnapshot,
                item.Puntuacion,
                item.Comentario,
                item.Estado,
                item.FechaCreacion,
                item.FechaAlertaOperativa,
                item.MotivoAlertaOperativa
            })
            .ToArrayAsync(cancellationToken);

        return items
            .Select(item => new CentroOperativoResenaBajaDto(
                item.IdResenaNegocio,
                item.IdCita,
                item.CodigoCita,
                Display(item.Cliente, "Cliente"),
                Display(item.Servicio, "Servicio"),
                item.Prestador,
                item.Puntuacion,
                item.Comentario,
                item.Estado,
                item.FechaCreacion,
                item.FechaAlertaOperativa,
                item.MotivoAlertaOperativa,
                PrioridadCritica,
                "Revisar la experiencia, contactar al cliente si corresponde y responder la reseña."))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<CentroOperativoPagoPendienteDto>> GetPagosPendientesAsync(
        IQueryable<Pago> query,
        DateTime now,
        int limit,
        CancellationToken cancellationToken)
    {
        var items = await query
            .OrderBy(item => item.FechaExpiracion ?? DateTime.MaxValue)
            .ThenBy(item => item.FechaCreacion)
            .Take(limit)
            .Select(item => new
            {
                item.IdPago,
                item.IdCita,
                CodigoCita = item.Cita.Codigo,
                Cliente = item.Cita.Cliente.Nombre,
                Servicio = item.Cita.Servicio.Nombre,
                item.Monto,
                item.Moneda,
                EstadoPago = item.EstadoPago.Nombre,
                MetodoPago = item.MetodoPago.Nombre,
                item.EsManual,
                item.FechaCreacion,
                item.FechaExpiracion
            })
            .ToArrayAsync(cancellationToken);

        return items
            .Select(item =>
            {
                var estaVencido = item.FechaExpiracion.HasValue && item.FechaExpiracion <= now;
                return new CentroOperativoPagoPendienteDto(
                    item.IdPago,
                    item.IdCita,
                    item.CodigoCita,
                    item.Cliente,
                    item.Servicio,
                    item.Monto,
                    item.Moneda,
                    item.EstadoPago,
                    item.MetodoPago,
                    item.EsManual,
                    item.FechaCreacion,
                    item.FechaExpiracion,
                    estaVencido,
                    estaVencido ? PrioridadAlta : PrioridadMedia,
                    estaVencido
                        ? "Revisar el pago vencido y generar una nueva orden o cancelar la reserva según corresponda."
                        : "Hacer seguimiento del pago pendiente antes de confirmar la atención.");
            })
            .ToArray();
    }

    private async Task<IReadOnlyCollection<CentroOperativoCitaSinConfirmarDto>> GetCitasSinConfirmarAsync(
        IQueryable<Cita> query,
        DateTime now,
        int limit,
        CancellationToken cancellationToken)
    {
        var items = await query
            .OrderBy(item => item.FechaInicio)
            .Take(limit)
            .Select(item => new
            {
                item.IdCita,
                CodigoCita = item.Codigo,
                Cliente = item.Cliente.Nombre,
                Servicio = item.Servicio.Nombre,
                Prestador = item.Prestador == null ? null : item.Prestador.Nombre,
                item.FechaInicio,
                item.FechaFin,
                EstadoCita = item.EstadoCita.Nombre,
                item.FechaCreacion
            })
            .ToArrayAsync(cancellationToken);

        return items
            .Select(item =>
            {
                var priority = item.FechaInicio <= now.AddHours(24) ? PrioridadAlta : PrioridadMedia;
                return new CentroOperativoCitaSinConfirmarDto(
                    item.IdCita,
                    item.CodigoCita,
                    item.Cliente,
                    item.Servicio,
                    item.Prestador,
                    item.FechaInicio,
                    item.FechaFin,
                    item.EstadoCita,
                    item.FechaCreacion,
                    priority,
                    "Confirmar, reagendar o cancelar la cita antes de que llegue la hora de atención.");
            })
            .ToArray();
    }

    private async Task<IReadOnlyCollection<CentroOperativoInvitacionVencidaDto>> GetInvitacionesVencidasAsync(
        IQueryable<InvitacionNegocio> query,
        DateTime now,
        int limit,
        CancellationToken cancellationToken)
    {
        var items = await query
            .OrderBy(item => item.FechaExpiracion)
            .Take(limit)
            .Select(item => new
            {
                item.IdInvitacionNegocio,
                item.Email,
                item.IdRolNegocio,
                RolNegocio = item.RolNegocio.Nombre,
                item.Estado,
                item.FechaCreacion,
                item.FechaExpiracion
            })
            .ToArrayAsync(cancellationToken);

        return items
            .Select(item => new CentroOperativoInvitacionVencidaDto(
                item.IdInvitacionNegocio,
                item.Email,
                item.IdRolNegocio,
                item.RolNegocio,
                item.Estado,
                item.FechaCreacion,
                item.FechaExpiracion,
                Math.Max(0, (int)Math.Floor((now.Date - item.FechaExpiracion.Date).TotalDays)),
                PrioridadMedia,
                "Reenviar la invitación si la persona sigue vigente o cancelarla para mantener limpio el acceso."))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<CentroOperativoNotificacionErrorDto>> GetNotificacionesErrorAsync(
        IQueryable<Notificacion> query,
        int limit,
        CancellationToken cancellationToken)
    {
        var items = await query
            .OrderByDescending(item => item.FechaCreacion)
            .Take(limit)
            .Select(item => new
            {
                item.IdNotificacion,
                item.IdCita,
                CodigoCita = item.Cita == null ? null : item.Cita.Codigo,
                item.IdResenaNegocio,
                TipoNotificacion = item.TipoNotificacion.Nombre,
                CanalNotificacion = item.CanalNotificacion.Nombre,
                item.Destinatario,
                item.Asunto,
                item.Error,
                item.FechaProgramada,
                item.FechaCreacion
            })
            .ToArrayAsync(cancellationToken);

        return items
            .Select(item => new CentroOperativoNotificacionErrorDto(
                item.IdNotificacion,
                item.IdCita,
                item.CodigoCita,
                item.IdResenaNegocio,
                item.TipoNotificacion,
                item.CanalNotificacion,
                item.Destinatario,
                item.Asunto,
                item.Error,
                item.FechaProgramada,
                item.FechaCreacion,
                PrioridadAlta,
                "Corregir el destinatario o configuración del canal y reprocesar la notificación."))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<CentroOperativoSolicitudCambioDto>> GetSolicitudesCambioAsync(
        IQueryable<Cita> query,
        DateTime now,
        int limit,
        CancellationToken cancellationToken)
    {
        var items = await query
            .OrderByDescending(item => item.FechaActualizacion ?? item.FechaCreacion)
            .ThenBy(item => item.FechaInicio)
            .Take(limit)
            .Select(item => new
            {
                item.IdCita,
                CodigoCita = item.Codigo,
                Cliente = item.Cliente.Nombre,
                Servicio = item.Servicio.Nombre,
                Prestador = item.Prestador == null ? null : item.Prestador.Nombre,
                item.FechaInicio,
                item.FechaFin,
                EstadoCita = item.EstadoCita.Nombre,
                item.FechaActualizacion,
                UltimaObservacion = item.Historial
                    .OrderByDescending(historial => historial.FechaCreacion)
                    .Select(historial => historial.Observacion)
                    .FirstOrDefault()
            })
            .ToArrayAsync(cancellationToken);

        return items
            .Select(item =>
            {
                var priority = item.FechaInicio <= now.AddHours(24) ? PrioridadAlta : PrioridadMedia;
                return new CentroOperativoSolicitudCambioDto(
                    item.IdCita,
                    item.CodigoCita,
                    item.Cliente,
                    item.Servicio,
                    item.Prestador,
                    item.FechaInicio,
                    item.FechaFin,
                    item.EstadoCita,
                    item.FechaActualizacion,
                    item.UltimaObservacion,
                    priority,
                    "Validar el cambio con el cliente y dejar la cita confirmada o con seguimiento claro.");
            })
            .ToArray();
    }

    private async Task<bool> CanViewAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsSuperAdmin)
        {
            return true;
        }

        if (!currentUser.IsAuthenticated)
        {
            return false;
        }

        return await dbContext.NegocioUsuarios.AnyAsync(
            item =>
                item.IdNegocio == idNegocio &&
                item.UserId == currentUser.UserId &&
                item.Activo &&
                (item.RolNegocio.Nombre == OwnerRoleName || item.RolNegocio.Nombre == AdminRoleName),
            cancellationToken);
    }

    private static string Display(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
