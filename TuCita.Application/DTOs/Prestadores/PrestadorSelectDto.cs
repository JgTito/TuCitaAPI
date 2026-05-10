namespace TuCita.Application.Prestadores;

public sealed record PrestadorSelectDto(
    int IdPrestador,
    int IdNegocio,
    string Negocio,
    string Label,
    string Nombre,
    int IdTipoPrestador,
    string TipoPrestador,
    string? UserId,
    int Capacidad,
    bool Activo);
