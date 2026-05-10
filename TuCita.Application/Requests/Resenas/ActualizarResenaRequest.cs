using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Resenas;

public sealed record ActualizarResenaRequest(
    [Range(1, 5)] byte Puntuacion,
    [MaxLength(1500)] string? Comentario);
