using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Citas;

public sealed class CitaQuery
{
    private const int MaxPageSize = 100;
    private int _pageNumber = 1;
    private int _pageSize = 20;

    [MaxLength(100)]
    public string? Search { get; init; }

    public int? IdCliente { get; init; }

    public int? IdServicio { get; init; }

    public int? IdPrestador { get; init; }

    public int? IdEstadoCita { get; init; }

    public DateTime? FechaDesde { get; init; }

    public DateTime? FechaHasta { get; init; }

    public bool? SoloEstadosActivos { get; init; }

    [Range(1, int.MaxValue)]
    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1 ? 1 : value;
    }

    [Range(1, MaxPageSize)]
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }
}
