namespace TuCita.Application.InformesInteligentes;

public sealed record InformeInteligenteNegocioDto(
    int IdNegocio,
    string Nombre,
    string Slug,
    string Rubro,
    bool Activo);
