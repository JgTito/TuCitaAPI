namespace TuCita.Application.Resenas;

public sealed record ReputacionPrestadorDto(
    int? IdPrestador,
    string Prestador,
    decimal PromedioPuntuacion,
    int TotalResenas,
    int TotalResenasPositivas,
    decimal PorcentajeResenasPositivas);
