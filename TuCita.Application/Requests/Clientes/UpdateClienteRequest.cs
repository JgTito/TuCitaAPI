using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Clientes;

public sealed record UpdateClienteRequest(
    [MaxLength(128)] string? UserId,
    [Required, MaxLength(150)] string Nombre,
    [MaxLength(30)] string? Telefono,
    [EmailAddress, MaxLength(150)] string? Email,
    [MaxLength(20)] string? Rut,
    [MaxLength(1000)] string? Notas,
    bool Activo);
