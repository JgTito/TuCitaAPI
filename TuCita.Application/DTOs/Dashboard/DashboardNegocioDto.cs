namespace TuCita.Application.Dashboard;

public sealed record DashboardNegocioDto(
    int IdNegocio,
    string Negocio,
    DateTime FechaDesde,
    DateTime FechaHasta,
    DashboardResumenDto Resumen,
    IReadOnlyCollection<DashboardEstadoCitaSerieDto> CitasPorEstado,
    IReadOnlyCollection<DashboardCitasPorDiaDto> CitasPorDia,
    IReadOnlyCollection<DashboardPrestadorOcupacionDto> OcupacionPorPrestador,
    IReadOnlyCollection<DashboardProximaCitaDto> ProximasCitas,
    IReadOnlyCollection<DashboardResenaPuntuacionDto> ResenasPorPuntuacion,
    IReadOnlyCollection<DashboardResenaRecienteDto> UltimasResenas);
