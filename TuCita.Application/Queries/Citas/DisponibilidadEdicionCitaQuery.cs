using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Citas;

public sealed class DisponibilidadEdicionCitaQuery
{
    public int? IdServicio { get; init; }

    public int? IdPrestador { get; init; }

    [Required]
    public DateOnly Fecha { get; init; }

    [Range(5, 120)]
    public int IntervaloMinutos { get; init; } = 15;
}
