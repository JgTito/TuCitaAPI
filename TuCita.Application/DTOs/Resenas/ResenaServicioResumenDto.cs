namespace TuCita.Application.Resenas;

public sealed record ResenaServicioResumenDto(
    int IdServicio,
    string Servicio,
    decimal PromedioPuntuacion,
    int TotalResenas);
