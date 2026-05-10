using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.TiposNotificacion;

public sealed record UpdateTipoNotificacionRequest(
    [Required, MaxLength(100)] string Nombre,
    [MaxLength(300)] string? Descripcion,
    bool Activo);
