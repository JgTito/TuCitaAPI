namespace TuCita.Application.Agenda;

public sealed record MiAgendaPrestadorDto(
    int IdNegocio,
    string Negocio,
    int IdPrestador,
    string Prestador,
    int IdTipoPrestador,
    string TipoPrestador);
