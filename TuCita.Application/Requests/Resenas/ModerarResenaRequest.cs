using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Resenas;

public sealed record ModerarResenaRequest(
    [MaxLength(300)] string? Motivo);
