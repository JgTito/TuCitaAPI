namespace TuCita.Application.Dashboard;

public sealed record DashboardUltimoPagoDto(
    int IdPago,
    int IdCita,
    string CodigoCita,
    string Cliente,
    string Servicio,
    string Proveedor,
    string CommerceOrder,
    decimal Monto,
    decimal MontoDevuelto,
    decimal MontoNeto,
    string Moneda,
    int IdEstadoPago,
    string EstadoPago,
    DateTime FechaCreacion,
    DateTime? FechaPago,
    DateTime? FechaActualizacion);
