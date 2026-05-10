namespace TuCita.Api.BackgroundJobs;

public sealed class BackgroundJobsOptions
{
    public const string SectionName = "BackgroundJobs";

    public bool Enabled { get; init; } = true;
    public bool RunOnStartup { get; init; } = true;
    public int IntervalSeconds { get; init; } = 60;
    public int MaxNotificacionesPorCiclo { get; init; } = 100;
    public int MaxPagosFlowPorCiclo { get; init; } = 50;
    public bool ProcesarNotificaciones { get; init; } = true;
    public bool SincronizarPagosFlow { get; init; } = true;
    public bool ExpirarPagos { get; init; } = true;
    public bool ExpirarInvitaciones { get; init; } = true;
    public bool ExpirarSolicitudesResena { get; init; } = true;
}
