namespace TuCita.Application.InformesInteligentes;

public sealed record InformeInteligenteEstadoDto(
    int IdEstadoCita,
    string Estado,
    bool EsEstadoFinal,
    int TotalCitas,
    decimal Porcentaje);
