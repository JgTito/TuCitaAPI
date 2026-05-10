namespace TuCita.Application.Pagos;

public sealed record CrearPagoFlowResponseDto(
    int IdPago,
    int IdCita,
    string CodigoCita,
    string CommerceOrder,
    long? FlowOrder,
    string EstadoPago,
    decimal Monto,
    string Moneda,
    string CheckoutUrl,
    DateTime? FechaExpiracion);
