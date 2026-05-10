namespace TuCita.Application.Dashboard;

public sealed record DashboardPagosPorDiaDto(
    DateTime Fecha,
    int Total,
    int Pagados,
    int Pendientes,
    int Rechazados,
    int Anulados,
    int ParcialmenteDevueltos,
    int Devueltos,
    int ConError,
    decimal MontoPagado,
    decimal MontoDevuelto,
    decimal MontoPendiente,
    decimal MontoRechazado);
