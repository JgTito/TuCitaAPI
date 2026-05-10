namespace TuCita.Application.Pagos;

public sealed record PagoServicioFiltroDto(
    int IdServicio,
    string Label,
    string Nombre,
    bool RequierePagoAnticipado,
    int CantidadPagos);
