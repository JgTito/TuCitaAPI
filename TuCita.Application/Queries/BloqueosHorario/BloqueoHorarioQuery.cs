using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.BloqueosHorario;

public sealed class BloqueoHorarioQuery
{
    private const int MaxPageSize = 100;
    private int _pageNumber = 1;
    private int _pageSize = 20;

    public int? IdPrestador { get; init; }

    public DateTime? FechaDesde { get; init; }

    public DateTime? FechaHasta { get; init; }

    [MaxLength(150)]
    public string? Search { get; init; }

    public bool? Activo { get; init; }

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
