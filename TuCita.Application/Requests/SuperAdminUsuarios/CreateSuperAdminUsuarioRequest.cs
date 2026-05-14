using System.ComponentModel.DataAnnotations;
using TuCita.Application.Common;

namespace TuCita.Application.SuperAdminUsuarios;

public sealed record CreateSuperAdminUsuarioRequest(
    [RequiredNonWhiteSpace, EmailAddress, MaxLength(256)] string Email,
    [MaxLength(256)] string? UserName,
    [RequiredNonWhiteSpace, MinLength(6), MaxLength(100)] string Password,
    [RequiredNonWhiteSpace, MaxLength(100)] string ConfirmPassword,
    [MaxLength(30)] string? PhoneNumber,
    bool EmailConfirmed,
    [MaxLength(100)] string? Nombre,
    [MaxLength(100)] string? Apellido,
    [MaxLength(20)] string? Rut,
    DateTime? FechaNacimiento,
    bool Activo = true,
    IReadOnlyCollection<string>? Roles = null);
