namespace TuCita.Application.Clientes;

public sealed record ClienteSelectDto(
    int IdCliente,
    string Label,
    string Nombre,
    string? Email,
    string? Telefono,
    string? Rut,
    string? UserId,
    bool Activo);
