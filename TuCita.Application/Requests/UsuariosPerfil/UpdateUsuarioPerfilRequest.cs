using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.UsuariosPerfil;

public sealed record UpdateUsuarioPerfilRequest(
    [MaxLength(100)] string? Nombre,
    [MaxLength(100)] string? Apellido,
    [MaxLength(20)] string? Rut,
    DateTime? FechaNacimiento,
    [MaxLength(30)] string? TelefonoAlternativo,
    [MaxLength(300)] string? Direccion,
    int? IdComuna);
