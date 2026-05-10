using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Citas;

public sealed record UpdateNotaInternaCitaRequest(
    [MaxLength(1000)] string? NotaInterna,
    [MaxLength(1000)] string? Observacion);
