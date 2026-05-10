namespace TuCita.Application.CamposReserva;

public sealed record CampoReservaDto(
    int IdCampoReserva,
    int IdNegocio,
    string Negocio,
    int? IdServicio,
    string? Servicio,
    int IdTipoCampo,
    string TipoCampo,
    string NombreInterno,
    string Etiqueta,
    string? Placeholder,
    string? TextoAyuda,
    bool Obligatorio,
    int Orden,
    bool Activo);
