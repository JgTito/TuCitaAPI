namespace TuCita.Application.ReservasPublicas;

public sealed record PublicNegocioDto(
    int IdNegocio,
    string Rubro,
    string Nombre,
    string Slug,
    string? Descripcion,
    string? LogoUrl,
    string? Direccion,
    string? Telefono,
    string? Email);
