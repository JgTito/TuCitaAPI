namespace TuCita.Application.ReservasPublicas;

public sealed record PublicCampoReservaOpcionDto(
    int IdCampoReservaOpcion,
    string Etiqueta,
    string Valor,
    int Orden);
