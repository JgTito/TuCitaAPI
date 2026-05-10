namespace TuCita.Application.Pagos;

public sealed record PagoComprobanteDto(
    string FileName,
    string ContentType,
    byte[] Content);
