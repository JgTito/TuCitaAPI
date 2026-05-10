namespace TuCita.Application.Clientes;

public sealed record ClienteReservaDto(
    int IdCliente,
    int IdNegocio,
    string? UserId,
    string Nombre,
    string? Email,
    string? Telefono,
    string? Rut);
