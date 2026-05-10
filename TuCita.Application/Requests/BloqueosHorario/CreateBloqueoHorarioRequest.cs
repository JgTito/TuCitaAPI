using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.BloqueosHorario;

public sealed record CreateBloqueoHorarioRequest(
    int? IdPrestador,
    [Required] DateTime FechaInicio,
    [Required] DateTime FechaFin,
    [MaxLength(300)] string? Motivo,
    bool Activo = true);
