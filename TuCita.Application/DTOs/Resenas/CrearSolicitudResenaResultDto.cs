namespace TuCita.Application.Resenas;

public sealed record CrearSolicitudResenaResultDto(
    bool Creada,
    int? IdSolicitudResena,
    string? Destinatario);
