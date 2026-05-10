using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Negocios;

public sealed record CreateNegocioRequest(
    [Required] int IdRubro,
    [Required, MaxLength(150)] string Nombre,
    [Required, MaxLength(150)] string Slug,
    [MaxLength(500)] string? Descripcion,
    [MaxLength(500)] string? LogoUrl,
    [MaxLength(300)] string? Direccion,
    [MaxLength(30)] string? Telefono,
    [EmailAddress, MaxLength(150)] string? Email,
    bool Activo = true);
