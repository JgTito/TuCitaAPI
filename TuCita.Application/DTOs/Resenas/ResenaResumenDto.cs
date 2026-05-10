namespace TuCita.Application.Resenas;

public sealed record ResenaResumenDto(
    int IdNegocio,
    string Negocio,
    decimal PromedioPuntuacion,
    int TotalResenas,
    int TotalPublicadas,
    int TotalPendientes,
    int TotalRechazadas,
    int TotalOcultas,
    IReadOnlyCollection<ResenaPuntuacionDistribucionDto> Distribucion,
    IReadOnlyCollection<ResenaServicioResumenDto> ServiciosMejorEvaluados,
    IReadOnlyCollection<ResenaPrestadorResumenDto> PrestadoresMejorEvaluados,
    IReadOnlyCollection<ResenaPublicaDto> UltimasResenas);
