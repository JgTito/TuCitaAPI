using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Citas;

public sealed record ReagendarCitaRequest(
    [Required] DateTime FechaInicio,
    DateTime? FechaFin,
    int? IdPrestador,
    [MaxLength(1000)] string? Observacion);
