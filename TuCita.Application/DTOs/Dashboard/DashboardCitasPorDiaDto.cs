namespace TuCita.Application.Dashboard;

public sealed record DashboardCitasPorDiaDto(
    DateTime Fecha,
    int Total,
    int Pendientes,
    int Confirmadas,
    int Canceladas,
    int Atendidas,
    int NoAsistidas,
    decimal IngresosEstimados);
