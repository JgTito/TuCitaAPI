namespace TuCita.Application.ReservasPublicas;

public sealed record PublicNegocioEstrellaDto(
    byte Puntuacion,
    int Cantidad,
    decimal Porcentaje);
