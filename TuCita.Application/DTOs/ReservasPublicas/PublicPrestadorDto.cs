namespace TuCita.Application.ReservasPublicas;

public sealed record PublicPrestadorDto(
    int IdPrestador,
    string TipoPrestador,
    string Nombre,
    int Capacidad);
