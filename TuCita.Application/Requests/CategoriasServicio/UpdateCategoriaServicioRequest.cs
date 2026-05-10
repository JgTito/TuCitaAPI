using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.CategoriasServicio;

public sealed record UpdateCategoriaServicioRequest(
    [Required, MaxLength(100)] string Nombre,
    [MaxLength(300)] string? Descripcion,
    bool Activo);
