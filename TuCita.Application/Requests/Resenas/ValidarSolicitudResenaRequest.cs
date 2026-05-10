using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Resenas;

public sealed record ValidarSolicitudResenaRequest(
    [Required, MaxLength(500)] string Token);
