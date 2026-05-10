using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.RolesNegocio;

public sealed record UpdateRolNegocioRequest(
    [Required, MaxLength(80)] string Nombre,
    [MaxLength(300)] string? Descripcion,
    bool Activo);
