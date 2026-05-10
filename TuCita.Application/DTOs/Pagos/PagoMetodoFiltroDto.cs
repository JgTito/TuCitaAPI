namespace TuCita.Application.Pagos;

public sealed record PagoMetodoFiltroDto(
    int IdMetodoPago,
    string Label,
    bool EsManual,
    bool EsOnline,
    int CantidadPagos);
