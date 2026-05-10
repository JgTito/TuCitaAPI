namespace TuCita.Application.Citas;

public sealed record CitaHistorialEventoDto(
    int IdCitaHistorial,
    string TipoEvento,
    string Titulo,
    string? Descripcion,
    int? IdEstadoAnterior,
    string? EstadoAnterior,
    int IdEstadoNuevo,
    string EstadoNuevo,
    bool EsCambioEstado,
    string Actor,
    string? UserId,
    string? Usuario,
    DateTime FechaCreacion);
