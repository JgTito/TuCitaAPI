using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.EstadosNotificacion;

public sealed record CreateEstadoNotificacionRequest(
    [Required, MaxLength(80)] string Nombre,
    [MaxLength(300)] string? Descripcion,
    bool Activo = true);
