namespace TuCita.Application.ReservasPublicas;

public sealed record PublicNegocioSearchDto(
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
    int ServiciosActivos,
    decimal PromedioPuntuacion,
    int TotalResenas,
    int TotalPublicadas,
    IReadOnlyCollection<PublicNegocioEstrellaDto> DistribucionEstrellas);
