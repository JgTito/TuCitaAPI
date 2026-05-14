namespace TuCita.Application.InformesInteligentes;

public sealed record InformeInteligentePeriodoDto(
    DateTime FechaDesde,
    DateTime FechaHasta,
    int Dias,
    string Etiqueta);
