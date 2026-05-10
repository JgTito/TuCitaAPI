namespace TuCita.Application.ReservasPublicas;

public sealed record PublicReservaMisDatosDto(
    string? NombreCliente,
    string Email,
    string? Telefono,
    string? Rut,
    bool EmailBloqueado);
