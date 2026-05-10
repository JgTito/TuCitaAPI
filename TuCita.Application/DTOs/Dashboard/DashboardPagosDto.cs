namespace TuCita.Application.Dashboard;

public sealed record DashboardPagosDto(
    int IdNegocio,
    string Negocio,
    DateTime FechaDesde,
    DateTime FechaHasta,
    DashboardPagosResumenDto Resumen,
    IReadOnlyCollection<DashboardPagoEstadoDto> PagosPorEstado,
    IReadOnlyCollection<DashboardPagosPorDiaDto> PagosPorDia,
    IReadOnlyCollection<DashboardUltimoPagoDto> UltimosPagos);
