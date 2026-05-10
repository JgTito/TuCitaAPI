using System.ComponentModel.DataAnnotations;
using TuCita.Application.Common;

namespace TuCita.Application.Onboarding;

public sealed record RegisterDuenoNegocioRequest(
    [RequiredNonWhiteSpace, EmailAddress] string EmailUsuario,
    [RequiredNonWhiteSpace, MinLength(6)] string Password,
    [Required] int IdRubro,
    [RequiredNonWhiteSpace, MaxLength(150)] string Nombre,
    [RequiredNonWhiteSpace, MaxLength(150)] string Slug,
    [RequiredNonWhiteSpace, MaxLength(500)] string? Descripcion,
    [MaxLength(500)] string? LogoUrl,
    [RequiredNonWhiteSpace, MaxLength(300)] string? Direccion,
    [RequiredNonWhiteSpace, MaxLength(30)] string? Telefono,
    [RequiredNonWhiteSpace, EmailAddress, MaxLength(150)] string? EmailNegocio,
    bool Activo = true,
    [RequiredNonWhiteSpace, MaxLength(100)] string? NombreUsuario = null,
    [RequiredNonWhiteSpace, MaxLength(100)] string? ApellidoUsuario = null,
    [RequiredNonWhiteSpace, MaxLength(20)] string? Rut = null,
    [Required] DateTime? FechaNacimiento = null,
    [RequiredNonWhiteSpace, MaxLength(30)] string? TelefonoAlternativo = null,
    [RequiredNonWhiteSpace, MaxLength(300)] string? DireccionUsuario = null,
    [Required] int? IdComuna = null,
    [RequiredTrue(ErrorMessage = "Debes aceptar los términos y condiciones para registrarte.")] bool AceptaTerminos = false,
    bool AceptaMarketing = false);
