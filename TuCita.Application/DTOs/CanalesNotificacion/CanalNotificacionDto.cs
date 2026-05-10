namespace TuCita.Application.CanalesNotificacion;

public sealed record CanalNotificacionDto(
    int IdCanalNotificacion,
    string Nombre,
    string? Descripcion,
    bool Activo);
