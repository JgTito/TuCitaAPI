using System.ComponentModel.DataAnnotations;
using TuCita.Application.Common;

namespace TuCita.Application.Pagos;

public sealed record RegistrarDevolucionPagoRequest(
    [Range(1, 999999999)]
    decimal Monto,
    [RequiredNonWhiteSpace]
    [MaxLength(500)]
    string Motivo,
    [MaxLength(100)]
    string? Referencia = null,
    DateTime? FechaDevolucion = null);
