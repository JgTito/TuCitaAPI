namespace TuCita.Application.Agenda;

public sealed record MiAgendaEventoDto(
    string Tipo,
    int? Id,
    string Titulo,
    int IdNegocio,
    string Negocio,
    DateTime FechaInicio,
    DateTime FechaFin,
    int? IdCita,
    int? IdCliente,
    string? Cliente,
    int? IdServicio,
    string? Servicio,
    int? IdPrestador,
    string? Prestador,
    string? Estado,
    string Color);
