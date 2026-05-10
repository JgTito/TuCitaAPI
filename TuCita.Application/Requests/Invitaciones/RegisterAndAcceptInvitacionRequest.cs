using System.ComponentModel.DataAnnotations;
using TuCita.Application.Common;

namespace TuCita.Application.Invitaciones;

public sealed class RegisterAndAcceptInvitacionRequest
{
    [RequiredNonWhiteSpace]
    public string Token { get; init; } = string.Empty;

    [RequiredNonWhiteSpace]
    [MinLength(6)]
    public string Password { get; init; } = string.Empty;

    [RequiredNonWhiteSpace]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; init; } = string.Empty;

    [RequiredNonWhiteSpace]
    [MaxLength(100)]
    public string Nombre { get; init; } = string.Empty;

    [RequiredNonWhiteSpace]
    [MaxLength(100)]
    public string Apellido { get; init; } = string.Empty;

    [RequiredNonWhiteSpace]
    [MaxLength(20)]
    public string Rut { get; init; } = string.Empty;

    [Required]
    public DateTime? FechaNacimiento { get; init; }

    [RequiredNonWhiteSpace]
    [MaxLength(30)]
    public string TelefonoAlternativo { get; init; } = string.Empty;

    [RequiredNonWhiteSpace]
    [MaxLength(300)]
    public string Direccion { get; init; } = string.Empty;

    [Required]
    public int? IdComuna { get; init; }

    [RequiredTrue(ErrorMessage = "Debes aceptar los términos y condiciones para registrarte.")]
    public bool AceptaTerminos { get; init; }

    public bool AceptaMarketing { get; init; }
}
