namespace TuCita.Application.Citas;

public sealed record CitaHistorialDto(
    int IdCitaHistorial,
    int? IdEstadoAnterior,
    string? EstadoAnterior,
    int IdEstadoNuevo,
    string EstadoNuevo,
    string? UserId,
    string? Usuario,
    string? Observacion,
    DateTime FechaCreacion);
