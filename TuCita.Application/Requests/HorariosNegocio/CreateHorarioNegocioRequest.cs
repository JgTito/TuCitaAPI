using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.HorariosNegocio;

public sealed record CreateHorarioNegocioRequest(
    [Range(1, 7)] byte DiaSemana,
    [Required] TimeOnly HoraInicio,
    [Required] TimeOnly HoraFin,
    bool Activo = true);
