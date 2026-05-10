namespace TuCita.Application.TiposNotificacion;

public sealed record TipoNotificacionDto(
    int IdTipoNotificacion,
    string Nombre,
    string? Descripcion,
    bool Activo);
