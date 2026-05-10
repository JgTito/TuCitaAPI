using System.ComponentModel.DataAnnotations;
using TuCita.Application.Common;

namespace TuCita.Api.Requests.Onboarding;

public sealed class RegisterDuenoNegocioFormRequest
{
    [RequiredNonWhiteSpace, EmailAddress]
    public string EmailUsuario { get; init; } = string.Empty;

    [RequiredNonWhiteSpace, MinLength(6)]
    public string Password { get; init; } = string.Empty;

    [Required]
    public int IdRubro { get; init; }

    [RequiredNonWhiteSpace, MaxLength(150)]
    public string Nombre { get; init; } = string.Empty;

    [RequiredNonWhiteSpace, MaxLength(150)]
    public string Slug { get; init; } = string.Empty;

    [RequiredNonWhiteSpace, MaxLength(500)]
    public string Descripcion { get; init; } = string.Empty;

    public IFormFile? Logo { get; init; }

    [RequiredNonWhiteSpace, MaxLength(300)]
    public string Direccion { get; init; } = string.Empty;

    [RequiredNonWhiteSpace, MaxLength(30)]
    public string Telefono { get; init; } = string.Empty;

    [RequiredNonWhiteSpace, EmailAddress, MaxLength(150)]
    public string EmailNegocio { get; init; } = string.Empty;

    public bool Activo { get; init; } = true;

    [RequiredNonWhiteSpace, MaxLength(100)]
    public string NombreUsuario { get; init; } = string.Empty;

    [RequiredNonWhiteSpace, MaxLength(100)]
    public string ApellidoUsuario { get; init; } = string.Empty;

    [RequiredNonWhiteSpace, MaxLength(20)]
    public string Rut { get; init; } = string.Empty;

    [Required]
    public DateTime? FechaNacimiento { get; init; }

    public IFormFile? Avatar { get; init; }

    [RequiredNonWhiteSpace, MaxLength(30)]
    public string TelefonoAlternativo { get; init; } = string.Empty;

    [RequiredNonWhiteSpace, MaxLength(300)]
    public string DireccionUsuario { get; init; } = string.Empty;

    [Required]
    public int? IdComuna { get; init; }

    [RequiredTrue(ErrorMessage = "Debes aceptar los términos y condiciones para registrarte.")]
    public bool AceptaTerminos { get; init; }

    public bool AceptaMarketing { get; init; }
}
