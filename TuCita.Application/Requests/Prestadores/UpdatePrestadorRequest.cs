using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Prestadores;

public sealed record UpdatePrestadorRequest(
    [Required] int IdTipoPrestador,
    [MaxLength(128)] string? UserId,
    [Required, MaxLength(150)] string Nombre,
    [EmailAddress, MaxLength(150)] string? Email,
    [MaxLength(30)] string? Telefono,
    [Range(1, int.MaxValue)] int Capacidad,
    bool Activo);
