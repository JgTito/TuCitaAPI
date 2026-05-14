namespace TuCita.Application.InformesInteligentes;

public sealed record InformeInteligenteClienteSegmentoDto(
    string Segmento,
    int TotalCitas,
    int ClientesUnicos,
    int CitasAtendidas,
    int CitasCanceladas,
    int CitasNoAsistidas,
    decimal TasaNoAsistencia,
    decimal IngresosEstimados);
