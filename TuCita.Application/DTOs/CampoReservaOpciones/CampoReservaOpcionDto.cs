namespace TuCita.Application.CampoReservaOpciones;

public sealed record CampoReservaOpcionDto(
    int IdCampoReservaOpcion,
    int IdNegocio,
    int IdCampoReserva,
    string CampoReserva,
    string Etiqueta,
    string Valor,
    int Orden,
    bool Activo);
