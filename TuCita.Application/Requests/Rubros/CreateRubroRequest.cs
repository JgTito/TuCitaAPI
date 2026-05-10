using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Rubros;

public sealed record CreateRubroRequest(
    [Required, MaxLength(100)] string Nombre,
    [MaxLength(300)] string? Descripcion,
    bool Activo = true);
