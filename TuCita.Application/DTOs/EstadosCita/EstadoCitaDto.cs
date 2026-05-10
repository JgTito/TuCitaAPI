namespace TuCita.Application.EstadosCita;

public sealed record EstadoCitaDto(
    int IdEstadoCita,
    string Nombre,
    string? Descripcion,
    bool EsEstadoFinal,
    bool Activo);
