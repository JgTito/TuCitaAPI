using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Agenda;

public sealed class MiAgendaQuery
{
    [Required]
    public DateOnly? FechaDesde { get; init; }

    public DateOnly? FechaHasta { get; init; }

    public int? IdNegocio { get; init; }

    public int? IdServicio { get; init; }

    public int? IdPrestador { get; init; }

    public int? IdEstadoCita { get; init; }

    [MaxLength(100)]
    public string? Search { get; init; }

    public bool IncluirEstadosFinales { get; init; } = true;

    public bool IncluirDisponibilidad { get; init; } = true;

    public bool IncluirBloqueos { get; init; } = true;

    [Range(5, 120)]
    public int IntervaloMinutos { get; init; } = 15;
}
