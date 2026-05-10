namespace TuCita.Application.Pagos;

public sealed record EstadoPagoFiltroDto(
    int IdEstadoPago,
    string Label,
    bool EsEstadoFinal,
    int CantidadPagos);
