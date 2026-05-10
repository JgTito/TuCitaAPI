namespace TuCita.Application.UsuariosPerfil;

public sealed record UsuarioPerfilDto(
    int IdUsuarioPerfil,
    string UserId,
    string Email,
    string UserName,
    string? Nombre,
    string? Apellido,
    string? NombreCompleto,
    string? Rut,
    DateTime? FechaNacimiento,
    string? AvatarUrl,
    string? TelefonoAlternativo,
    string? Direccion,
    int? IdPais,
    string? Pais,
    int? IdCiudad,
    string? Ciudad,
    int? IdComuna,
    string? Comuna);
