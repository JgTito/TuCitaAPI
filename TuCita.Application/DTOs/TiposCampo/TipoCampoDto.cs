namespace TuCita.Application.TiposCampo;

public sealed record TipoCampoDto(
    int IdTipoCampo,
    string Nombre,
    string? Descripcion,
    bool Activo);
