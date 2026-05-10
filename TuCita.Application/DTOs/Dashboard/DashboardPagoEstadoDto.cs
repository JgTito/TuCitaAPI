namespace TuCita.Application.Dashboard;

public sealed record DashboardPagoEstadoDto(
    int IdEstadoPago,
    string EstadoPago,
    bool EsEstadoFinal,
    int Total,
    decimal Monto,
    decimal MontoDevuelto,
    decimal MontoNeto);
