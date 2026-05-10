using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.CanalesNotificacion;

public sealed record CreateCanalNotificacionRequest(
    [Required, MaxLength(80)] string Nombre,
    [MaxLength(300)] string? Descripcion,
    bool Activo = true);
