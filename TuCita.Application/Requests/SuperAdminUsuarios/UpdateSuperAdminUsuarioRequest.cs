using System.ComponentModel.DataAnnotations;
using TuCita.Application.Common;

namespace TuCita.Application.SuperAdminUsuarios;

public sealed record UpdateSuperAdminUsuarioRequest(
    [RequiredNonWhiteSpace, EmailAddress, MaxLength(256)] string Email,
    [RequiredNonWhiteSpace, MaxLength(256)] string UserName,
    [MaxLength(30)] string? PhoneNumber,
    bool EmailConfirmed,
    bool PhoneNumberConfirmed,
    bool LockoutEnabled,
    bool Activo,
    [MaxLength(100)] string? Nombre,
    [MaxLength(100)] string? Apellido,
    [MaxLength(20)] string? Rut,
    DateTime? FechaNacimiento);
