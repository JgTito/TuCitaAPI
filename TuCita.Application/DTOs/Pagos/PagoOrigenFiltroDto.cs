namespace TuCita.Application.Pagos;

public sealed record PagoOrigenFiltroDto(
    bool EsManual,
    string Label,
    int CantidadPagos);
