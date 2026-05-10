namespace TuCita.Application.Citas;

public sealed record CitaHistorialTimelineDto(
    int IdCita,
    int IdNegocio,
    string Negocio,
    string Codigo,
    int IdCliente,
    string Cliente,
    int IdServicio,
    string Servicio,
    int? IdPrestador,
    string? Prestador,
    int IdEstadoActual,
    string EstadoActual,
    DateTime FechaInicio,
    DateTime FechaFin,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion,
    IReadOnlyCollection<CitaHistorialEventoDto> Eventos);
