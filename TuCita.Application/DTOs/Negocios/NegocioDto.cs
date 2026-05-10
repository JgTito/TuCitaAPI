namespace TuCita.Application.Negocios;

public sealed record NegocioDto(
    int IdNegocio,
    int IdRubro,
    string Rubro,
    string Nombre,
    string Slug,
    string? Descripcion,
    string? LogoUrl,
    string? Direccion,
    string? Telefono,
    string? Email,
    bool Activo,
    DateTime FechaCreacion);
