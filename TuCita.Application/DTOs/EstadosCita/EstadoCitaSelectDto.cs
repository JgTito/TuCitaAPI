namespace TuCita.Application.EstadosCita;

public sealed record EstadoCitaSelectDto(
    int IdEstadoCita,
    string Label,
    bool EsEstadoFinal);
