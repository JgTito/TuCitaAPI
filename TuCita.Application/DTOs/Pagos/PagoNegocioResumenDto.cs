namespace TuCita.Application.Pagos;

public sealed record PagoNegocioResumenDto(
    int CantidadTotal,
    int CantidadPagados,
    int CantidadPendientes,
    int CantidadRechazados,
    int CantidadAnulados,
    int CantidadParcialmenteDevueltos,
    int CantidadDevueltos,
    int CantidadError,
    decimal MontoTotal,
    decimal MontoPagado,
    decimal MontoPendiente,
    decimal MontoRechazado,
    decimal MontoAnulado,
    decimal MontoDevuelto,
    decimal MontoError);
