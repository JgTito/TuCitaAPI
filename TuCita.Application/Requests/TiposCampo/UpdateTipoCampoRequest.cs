using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.TiposCampo;

public sealed record UpdateTipoCampoRequest(
    [Required, MaxLength(80)] string Nombre,
    [MaxLength(300)] string? Descripcion,
    bool Activo);
