namespace TuCita.Application.Pagos;

public sealed record PagoHistorialDto(
    int IdPagoHistorial,
    int IdPago,
    int IdNegocio,
    int IdCita,
    string TipoEvento,
    string? EstadoAnterior,
    string? EstadoNuevo,
    decimal? Monto,
    string? Motivo,
    string? Referencia,
    string? UserId,
    string? UsuarioEmail,
    string? DatosJson,
    DateTime FechaCreacion);
