namespace TuCita.Application.Resenas;

public sealed record ReputacionNegocioDto(
    int IdNegocio,
    string Negocio,
    DateTime FechaDesde,
    DateTime FechaHasta,
    bool IncluyeNoPublicadas,
    decimal PromedioGeneral,
    int TotalResenas,
    int TotalResenasPositivas,
    decimal PorcentajeResenasPositivas,
    IReadOnlyCollection<ReputacionServicioDto> PromedioPorServicio,
    IReadOnlyCollection<ReputacionPrestadorDto> PromedioPorPrestador,
    IReadOnlyCollection<ReputacionMensualDto> EvolucionMensual);
