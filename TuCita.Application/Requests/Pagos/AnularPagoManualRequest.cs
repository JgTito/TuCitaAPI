using System.ComponentModel.DataAnnotations;
using TuCita.Application.Common;

namespace TuCita.Application.Pagos;

public sealed record AnularPagoManualRequest(
    [RequiredNonWhiteSpace]
    [MaxLength(500)]
    string Motivo,
    [MaxLength(100)]
    string? Referencia = null);
