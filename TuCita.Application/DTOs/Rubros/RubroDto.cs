namespace TuCita.Application.Rubros;

public sealed record RubroDto(
    int IdRubro,
    string Nombre,
    string? Descripcion,
    bool Activo);
