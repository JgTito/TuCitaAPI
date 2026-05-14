namespace TuCita.Application.InformesInteligentes;

public sealed record InformeInteligenteHorarioDto(
    int HoraInicio,
    string Bloque,
    int TotalCitas,
    int CitasAtendidas,
    int CitasCanceladas,
    int CitasNoAsistidas,
    decimal HorasReservadas,
    decimal IngresosEstimados);
