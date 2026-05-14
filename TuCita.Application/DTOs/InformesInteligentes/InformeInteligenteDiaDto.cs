namespace TuCita.Application.InformesInteligentes;

public sealed record InformeInteligenteDiaDto(
    int DiaSemana,
    string Dia,
    int TotalCitas,
    int CitasCanceladas,
    int CitasNoAsistidas,
    decimal IngresosEstimados);
