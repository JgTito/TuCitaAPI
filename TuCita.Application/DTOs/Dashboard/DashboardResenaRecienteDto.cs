namespace TuCita.Application.Dashboard;

public sealed record DashboardResenaRecienteDto(
    int IdResenaNegocio,
    int IdCita,
    string CodigoCita,
    string Cliente,
    string Servicio,
    string? Prestador,
    byte Puntuacion,
    string Estado,
    bool EsVisiblePublicamente,
    DateTime FechaCreacion);
