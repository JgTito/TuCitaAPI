namespace TuCita.Application.CentroOperativo;

public sealed record CentroOperativoPagoPendienteDto(
    int IdPago,
    int IdCita,
    string CodigoCita,
    string Cliente,
    string Servicio,
    decimal Monto,
    string Moneda,
    string EstadoPago,
    string MetodoPago,
    bool EsManual,
    DateTime FechaCreacion,
    DateTime? FechaExpiracion,
    bool EstaVencido,
    string Prioridad,
    string AccionSugerida);
