using Microsoft.Extensions.Options;
using TuCita.Application.Common;
using TuCita.Application.Invitaciones;
using TuCita.Application.Notificaciones;
using TuCita.Application.Pagos;
using TuCita.Application.Resenas;
using TuCita.Infrastucture.Email;
using TuCita.Infrastucture.Pagos;

namespace TuCita.Api.BackgroundJobs;

public sealed class TuCitaBackgroundWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<BackgroundJobsOptions> options,
    IOptions<EmailOptions> emailOptions,
    IOptions<FlowOptions> flowOptions,
    ILogger<TuCitaBackgroundWorker> logger) : BackgroundService
{
    private static readonly CurrentUserContext SystemUser = new(
        string.Empty,
        ["SuperAdmin"]);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Los jobs en segundo plano de TuCita están deshabilitados.");
            return;
        }

        logger.LogInformation("Jobs en segundo plano de TuCita iniciados.");

        if (options.Value.RunOnStartup)
        {
            await RunCycleSafelyAsync(stoppingToken);
        }

        using var timer = new PeriodicTimer(GetInterval());
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCycleSafelyAsync(stoppingToken);
        }
    }

    private async Task RunCycleSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RunCycleAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error inesperado al ejecutar jobs en segundo plano de TuCita.");
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        var config = options.Value;
        using var scope = scopeFactory.CreateScope();

        var pagos = scope.ServiceProvider.GetRequiredService<IPagoFlowService>();
        if (config.SincronizarPagosFlow && flowOptions.Value.Enabled)
        {
            var result = await pagos.ProcesarPendientesFlowAsync(
                config.MaxPagosFlowPorCiclo,
                cancellationToken);

            if (result.Succeeded && result.Data is not null)
            {
                logger.LogInformation(
                    "Pagos Flow consultados. Total: {Total}. Exitosos: {Exitosos}. Error: {Errores}.",
                    result.Data.Consultados,
                    result.Data.Exitosos,
                    result.Data.ConError);
            }
            else
            {
                logger.LogWarning(
                    "No se pudieron consultar pagos Flow pendientes: {Errores}",
                    string.Join(" | ", result.Errors));
            }
        }

        if (config.ExpirarPagos)
        {
            var result = await pagos.ExpirarPendientesAsync(cancellationToken);
            if (result.Succeeded && result.Data is not null)
            {
                logger.LogInformation(
                    "Pagos pendientes expirados: {Total}. Citas canceladas por pago expirado: {CitasCanceladas}.",
                    result.Data.Expirados,
                    result.Data.CitasCanceladas);
            }
            else
            {
                logger.LogWarning(
                    "No se pudieron expirar pagos pendientes: {Errores}",
                    string.Join(" | ", result.Errors));
            }
        }

        if (config.ExpirarInvitaciones)
        {
            var invitaciones = scope.ServiceProvider.GetRequiredService<IInvitacionNegocioService>();
            var result = await invitaciones.ExpirarPendientesAsync(cancellationToken);
            if (result.Succeeded && result.Data is not null)
            {
                logger.LogInformation("Invitaciones pendientes expiradas: {Total}.", result.Data.Expiradas);
            }
            else
            {
                logger.LogWarning(
                    "No se pudieron expirar invitaciones pendientes: {Errores}",
                    string.Join(" | ", result.Errors));
            }
        }

        if (config.ExpirarSolicitudesResena)
        {
            var resenas = scope.ServiceProvider.GetRequiredService<IResenaNegocioService>();
            var result = await resenas.ExpirarSolicitudesPendientesAsync(cancellationToken);
            if (result.Succeeded && result.Data is not null)
            {
                logger.LogInformation("Solicitudes de reseña pendientes expiradas: {Total}.", result.Data.Expiradas);
            }
            else
            {
                logger.LogWarning(
                    "No se pudieron expirar solicitudes de reseña pendientes: {Errores}",
                    string.Join(" | ", result.Errors));
            }
        }

        if (config.ProcesarNotificaciones && emailOptions.Value.Enabled)
        {
            var notificaciones = scope.ServiceProvider.GetRequiredService<INotificacionService>();
            var result = await notificaciones.ProcesarPendientesAsync(
                SystemUser,
                idNegocio: null,
                maxNotificaciones: config.MaxNotificacionesPorCiclo,
                cancellationToken);

            if (result.Succeeded && result.Data is not null)
            {
                logger.LogInformation(
                    "Notificaciones procesadas. Total: {Total}. Enviadas: {Enviadas}. Error: {Errores}.",
                    result.Data.TotalProcesadas,
                    result.Data.TotalEnviadas,
                    result.Data.TotalConError);
            }
            else
            {
                logger.LogWarning(
                    "No se pudieron procesar notificaciones pendientes: {Errores}",
                    string.Join(" | ", result.Errors));
            }
        }
    }

    private TimeSpan GetInterval()
    {
        return TimeSpan.FromSeconds(Math.Max(15, options.Value.IntervalSeconds));
    }
}
