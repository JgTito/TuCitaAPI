using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Resenas;

public sealed record ResponderResenaRequest(
    [Required, MaxLength(1000)] string Respuesta);
