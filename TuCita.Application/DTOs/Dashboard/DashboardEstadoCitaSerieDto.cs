namespace TuCita.Application.Dashboard;

public sealed record DashboardEstadoCitaSerieDto(
    int IdEstadoCita,
    string Estado,
    bool EsEstadoFinal,
    int Total,
    decimal IngresosEstimados);
