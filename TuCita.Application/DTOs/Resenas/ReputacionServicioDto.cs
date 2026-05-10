namespace TuCita.Application.Resenas;

public sealed record ReputacionServicioDto(
    int IdServicio,
    string Servicio,
    decimal PromedioPuntuacion,
    int TotalResenas,
    int TotalResenasPositivas,
    decimal PorcentajeResenasPositivas);
