namespace TuCita.Application.ReservasPublicas;

public sealed record PublicReservaDto(
    int IdCita,
    int IdNegocio,
    string Negocio,
    int IdCliente,
    string Cliente,
    int IdServicio,
    string Servicio,
    int? IdPrestador,
    string? Prestador,
    int IdEstadoCita,
    string EstadoCita,
    string Codigo,
    DateTime FechaInicio,
    DateTime FechaFin,
    decimal PrecioEstimado);
