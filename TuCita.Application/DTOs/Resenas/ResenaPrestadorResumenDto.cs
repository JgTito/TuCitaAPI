namespace TuCita.Application.Resenas;

public sealed record ResenaPrestadorResumenDto(
    int IdPrestador,
    string Prestador,
    decimal PromedioPuntuacion,
    int TotalResenas);
