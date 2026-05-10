namespace TuCita.Application.HorariosPrestador;

public sealed record HorarioPrestadorDto(
    int IdHorarioPrestador,
    int IdNegocio,
    string Negocio,
    int IdPrestador,
    string Prestador,
    byte DiaSemana,
    string DiaSemanaNombre,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    bool Activo);
