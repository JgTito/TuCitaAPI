using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Citas;

public sealed record CitaActionRequest(
    [MaxLength(1000)] string? Observacion);
