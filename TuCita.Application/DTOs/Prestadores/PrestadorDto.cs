namespace TuCita.Application.Prestadores;

public sealed record PrestadorDto(
    int IdPrestador,
    int IdNegocio,
    string Negocio,
    int IdTipoPrestador,
    string TipoPrestador,
    string? UserId,
    string? UserName,
    string? UsuarioEmail,
    string Nombre,
    string? Email,
    string? Telefono,
    int Capacidad,
    bool Activo,
    DateTime FechaCreacion);
