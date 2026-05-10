using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Pagos;

public sealed record RegistrarPagoManualRequest(
    [Required] int IdCita,
    [Range(1, 999999999)] decimal Monto,
    [Range(1, int.MaxValue)] int IdMetodoPago,
    DateTime? FechaPago = null,
    [MaxLength(100)] string? Referencia = null,
    [MaxLength(500)] string? Observacion = null);
