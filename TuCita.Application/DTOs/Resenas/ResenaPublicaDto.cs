namespace TuCita.Application.Resenas;

public sealed record ResenaPublicaDto(
    int IdResenaNegocio,
    string Cliente,
    string Servicio,
    string? Prestador,
    byte Puntuacion,
    string? Comentario,
    string? RespuestaNegocio,
    DateTime FechaPublicacion,
    DateTime? FechaRespuesta);
