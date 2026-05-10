using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Resenas;

public sealed record CrearResenaPublicaRequest(
    [Required, MaxLength(500)] string Token,
    [Range(1, 5)] byte Puntuacion,
    [MaxLength(1500)] string? Comentario);
