namespace TuCita.Application.CategoriasServicio;

public sealed record CategoriaServicioDto(
    int IdCategoriaServicio,
    int IdNegocio,
    string Negocio,
    string Nombre,
    string? Descripcion,
    bool Activo);
