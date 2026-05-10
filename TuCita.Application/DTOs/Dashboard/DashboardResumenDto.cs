namespace TuCita.Application.Dashboard;

public sealed record DashboardResumenDto(
    int CitasHoy,
    int ProximasCitas,
    int CitasPendientes,
    int CitasCanceladas,
    int CitasNoAsistidas,
    int TotalCitasRango,
    decimal IngresosEstimados,
    decimal PromedioResenas,
    int TotalResenas,
    int ResenasPendientes);
