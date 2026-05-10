namespace TuCita.Application.Dashboard;

public sealed record DashboardPagosResumenDto(
    int TotalPagos,
    int PagosPagados,
    int PagosPendientes,
    int PagosRechazados,
    int PagosAnulados,
    int PagosParcialmenteDevueltos,
    int PagosDevueltos,
    int PagosConError,
    decimal MontoTotal,
    decimal MontoPagado,
    decimal MontoPendiente,
    decimal MontoRechazado,
    decimal MontoAnulado,
    decimal MontoDevuelto,
    decimal MontoError,
    decimal TicketPromedioPagado,
    decimal TasaPagosConfirmados,
    decimal IngresosEstimadosCitas,
    decimal DiferenciaEstimadoVsPagado);
