using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.TiposPrestador;

public sealed record CreateTipoPrestadorRequest(
    [Required, MaxLength(80)] string Nombre,
    [MaxLength(300)] string? Descripcion,
    bool Activo = true);
