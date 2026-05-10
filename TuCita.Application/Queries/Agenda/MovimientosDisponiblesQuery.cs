using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Agenda;

public sealed class MovimientosDisponiblesQuery
{
    [Required]
    public DateOnly? FechaDesde { get; init; }

    public DateOnly? FechaHasta { get; init; }

    public int? IdPrestador { get; init; }

    [Range(5, 120)]
    public int IntervaloMinutos { get; init; } = 15;
}
