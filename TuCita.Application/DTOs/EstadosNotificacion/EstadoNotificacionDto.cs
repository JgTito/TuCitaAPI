namespace TuCita.Application.EstadosNotificacion;

public sealed record EstadoNotificacionDto(
    int IdEstadoNotificacion,
    string Nombre,
    string? Descripcion,
    bool Activo);
