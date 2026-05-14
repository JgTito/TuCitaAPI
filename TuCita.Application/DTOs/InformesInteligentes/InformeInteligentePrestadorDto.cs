namespace TuCita.Application.InformesInteligentes;

public sealed record InformeInteligentePrestadorDto(
    int? IdPrestador,
    string Prestador,
    int TotalCitas,
    int CitasAtendidas,
    int CitasCanceladas,
    int CitasNoAsistidas,
    decimal HorasReservadas,
    decimal HorasDisponiblesEstimadas,
    decimal TasaOcupacion,
    decimal IngresosEstimados);
