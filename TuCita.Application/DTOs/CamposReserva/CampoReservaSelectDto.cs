namespace TuCita.Application.CamposReserva;

public sealed record CampoReservaSelectDto(
    int IdCampoReserva,
    string Label,
    string NombreInterno,
    string Etiqueta,
    int IdTipoCampo,
    string TipoCampo,
    int? IdServicio,
    string? Servicio,
    bool Obligatorio,
    int Orden,
    bool Activo);
