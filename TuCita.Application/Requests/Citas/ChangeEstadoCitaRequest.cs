using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Citas;

public sealed record ChangeEstadoCitaRequest(
    [Required] int IdEstadoCita,
    [MaxLength(1000)] string? Observacion);
