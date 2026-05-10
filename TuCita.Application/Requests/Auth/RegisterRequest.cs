using System.ComponentModel.DataAnnotations;
using TuCita.Application.Common;

namespace TuCita.Application.Auth;

public sealed record RegisterRequest(
    [RequiredNonWhiteSpace, EmailAddress] string Email,
    [RequiredNonWhiteSpace, MinLength(6)] string Password,
    [RequiredNonWhiteSpace, MaxLength(100)] string Nombre,
    [RequiredNonWhiteSpace, MaxLength(100)] string Apellido,
    [RequiredNonWhiteSpace, MaxLength(20)] string Rut,
    [Required] DateTime? FechaNacimiento,
    [RequiredNonWhiteSpace, MaxLength(30)] string TelefonoAlternativo,
    [RequiredNonWhiteSpace, MaxLength(300)] string Direccion,
    [Required] int? IdComuna,
    [RequiredTrue(ErrorMessage = "Debes aceptar los términos y condiciones para registrarte.")] bool AceptaTerminos = false,
    bool AceptaMarketing = false);
