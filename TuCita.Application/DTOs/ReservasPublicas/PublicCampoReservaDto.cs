namespace TuCita.Application.ReservasPublicas;

public sealed record PublicCampoReservaDto(
    int IdCampoReserva,
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
    IReadOnlyCollection<PublicCampoReservaOpcionDto> Opciones);
