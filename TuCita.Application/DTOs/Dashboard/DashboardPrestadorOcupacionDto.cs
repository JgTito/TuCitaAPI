namespace TuCita.Application.Dashboard;

public sealed record DashboardPrestadorOcupacionDto(
    int IdPrestador,
    string Prestador,
    string TipoPrestador,
    int TotalCitas,
    int MinutosReservados,
    int MinutosDisponibles,
    decimal PorcentajeOcupacion,
    decimal IngresosEstimados);
