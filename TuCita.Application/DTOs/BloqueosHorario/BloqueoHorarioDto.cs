namespace TuCita.Application.BloqueosHorario;

public sealed record BloqueoHorarioDto(
    int IdBloqueoHorario,
    int IdNegocio,
    string Negocio,
    int? IdPrestador,
    string? Prestador,
    DateTime FechaInicio,
    DateTime FechaFin,
    string? Motivo,
    bool Activo,
    DateTime FechaCreacion);
