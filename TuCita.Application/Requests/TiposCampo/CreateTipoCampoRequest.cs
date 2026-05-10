using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.TiposCampo;

public sealed record CreateTipoCampoRequest(
    [Required, MaxLength(80)] string Nombre,
    [MaxLength(300)] string? Descripcion,
    bool Activo = true);
