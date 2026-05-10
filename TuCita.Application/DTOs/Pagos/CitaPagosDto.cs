namespace TuCita.Application.Pagos;

public sealed record CitaPagosDto(
    int IdCita,
    int IdNegocio,
    string Negocio,
    string CodigoCita,
    int IdCliente,
    string Cliente,
    int IdServicio,
    string Servicio,
    int IdEstadoCita,
    string EstadoCita,
    bool RequierePagoAnticipado,
    decimal PrecioEstimado,
    decimal TotalPagado,
    decimal SaldoPendiente,
    string? UltimoEstadoPago,
    bool TienePagoConfirmado,
    bool TienePagoPendiente,
    IReadOnlyCollection<PagoFlowDto> Pagos);
