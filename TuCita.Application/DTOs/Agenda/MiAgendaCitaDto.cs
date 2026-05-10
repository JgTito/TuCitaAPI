namespace TuCita.Application.Agenda;

public sealed record MiAgendaCitaDto(
    int IdNegocio,
    string Negocio,
    int IdCita,
    string Codigo,
    int IdCliente,
    string Cliente,
    int IdServicio,
    string Servicio,
    int IdPrestador,
    string Prestador,
    int IdEstadoCita,
    string EstadoCita,
    bool EsEstadoFinal,
    DateTime FechaInicio,
    DateTime FechaFin,
    decimal PrecioEstimado,
    string? ComentarioCliente,
    string? NotaInterna);
