namespace TuCita.Application.TiposPrestador;

public sealed record TipoPrestadorDto(
    int IdTipoPrestador,
    string Nombre,
    string? Descripcion,
    bool Activo);
