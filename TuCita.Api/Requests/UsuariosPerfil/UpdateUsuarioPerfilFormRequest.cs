using System.ComponentModel.DataAnnotations;

namespace TuCita.Api.Requests.UsuariosPerfil;

public sealed class UpdateUsuarioPerfilFormRequest
{
    [MaxLength(100)]
    public string? Nombre { get; init; }

    [MaxLength(100)]
    public string? Apellido { get; init; }

    [MaxLength(20)]
    public string? Rut { get; init; }

    public DateTime? FechaNacimiento { get; init; }

    public IFormFile? Avatar { get; init; }

    [MaxLength(30)]
    public string? TelefonoAlternativo { get; init; }

    [MaxLength(300)]
    public string? Direccion { get; init; }

    public int? IdComuna { get; init; }
}
