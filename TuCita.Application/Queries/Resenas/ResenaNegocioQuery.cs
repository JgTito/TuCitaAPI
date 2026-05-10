using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Resenas;

public sealed class ResenaNegocioQuery
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    public string? Estado { get; init; }

    [Range(1, 5)]
    public byte? Puntuacion { get; init; }

    public int? IdServicio { get; init; }

    public int? IdPrestador { get; init; }

    public DateTime? FechaDesde { get; init; }

    public DateTime? FechaHasta { get; init; }

    public bool? SoloVisiblesPublicamente { get; init; }

    public bool? SoloAlertasOperativas { get; init; }
}
