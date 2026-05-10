namespace TuCita.Application.Pagos;

public sealed record PagoFlowResultadoDto(
    string CommerceOrder,
    int IdPago,
    int IdCita,
    string CodigoCita,
    string EstadoPago,
    int? FlowStatus,
    string? FlowStatusNombre,
    decimal Monto,
    string Moneda,
    DateTime? FechaPago,
    string? FrontendRedirectUrl);
