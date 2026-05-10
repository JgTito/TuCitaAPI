namespace TuCita.Application.Clientes;

public sealed record ClienteDto(
    int IdCliente,
    int IdNegocio,
    string Negocio,
    string? UserId,
    string? UserName,
    string? UsuarioEmail,
    string Nombre,
    string? Telefono,
    string? Email,
    string? Rut,
    string? Notas,
    bool Activo,
    DateTime FechaCreacion);
