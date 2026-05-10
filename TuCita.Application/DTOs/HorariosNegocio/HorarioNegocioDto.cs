namespace TuCita.Application.HorariosNegocio;

public sealed record HorarioNegocioDto(
    int IdHorarioNegocio,
    int IdNegocio,
    string Negocio,
    byte DiaSemana,
    string DiaSemanaNombre,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    bool Activo);
