using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.HorariosPrestador;

public sealed record CreateHorarioPrestadorRequest(
    [Range(1, 7)] byte DiaSemana,
    [Required] TimeOnly HoraInicio,
    [Required] TimeOnly HoraFin,
    bool Activo = true);
