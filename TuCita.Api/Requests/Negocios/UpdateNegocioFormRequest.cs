using System.ComponentModel.DataAnnotations;

namespace TuCita.Api.Requests.Negocios;

public sealed class UpdateNegocioFormRequest
{
    [Required]
    public int IdRubro { get; init; }

    [Required, MaxLength(150)]
    public string Nombre { get; init; } = string.Empty;

    [Required, MaxLength(150)]
    public string Slug { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; init; }

    public IFormFile? Logo { get; init; }

    [MaxLength(300)]
    public string? Direccion { get; init; }

    [MaxLength(30)]
    public string? Telefono { get; init; }

    [EmailAddress, MaxLength(150)]
    public string? Email { get; init; }

    public bool Activo { get; init; }
}
