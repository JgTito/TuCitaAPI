namespace TuCita.Application.Pagos;

public sealed record PagoCitaFiltroDto(
    int IdCita,
    string Label,
    string Codigo,
    string Cliente,
    string Servicio,
    DateTime FechaInicio,
    int CantidadPagos);
