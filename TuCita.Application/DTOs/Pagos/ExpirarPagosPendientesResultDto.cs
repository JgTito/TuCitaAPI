namespace TuCita.Application.Pagos;

public sealed record ExpirarPagosPendientesResultDto(
    int Expirados,
    int CitasCanceladas);
