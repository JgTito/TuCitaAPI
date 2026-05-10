namespace TuCita.Application.Dashboard;

public sealed record DashboardProximaCitaDto(
    int IdCita,
    string Codigo,
    DateTime FechaInicio,
    DateTime FechaFin,
    int IdCliente,
    string Cliente,
    int IdServicio,
    string Servicio,
    int? IdPrestador,
    string? Prestador,
    int IdEstadoCita,
    string Estado,
    decimal PrecioEstimado);
