namespace TuCita.Application.Resenas;

public sealed record SolicitudResenaPreviewDto(
    bool Valida,
    string? Estado,
    string? Negocio,
    string? Cliente,
    string? Servicio,
    string? Prestador,
    DateTime? FechaAtencion,
    DateTime? FechaExpiracion,
    string? Mensaje);
