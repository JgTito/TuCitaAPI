namespace TuCita.Application.Resenas;

public sealed record ReputacionMensualDto(
    int Anio,
    int Mes,
    string Periodo,
    decimal PromedioPuntuacion,
    int TotalResenas,
    int TotalResenasPositivas,
    decimal PorcentajeResenasPositivas);
