namespace TuCita.Application.Pagos;

public sealed record PagoClienteFiltroDto(
    int IdCliente,
    string Label,
    string Nombre,
    string? Email,
    int CantidadPagos);
