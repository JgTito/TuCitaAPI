using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Auditoria;

public sealed class AuditoriaFiltroSelectQuery
{
    private const int MaxPageSize = 100;
    private int _pageSize = 50;

    [MaxLength(150)]
    public string? Search { get; init; }

    [MaxLength(80)]
    public string? Categoria { get; init; }

    [MaxLength(80)]
    public string? Accion { get; init; }

    [MaxLength(120)]
    public string? Entidad { get; init; }

    [MaxLength(128)]
    public string? UserId { get; init; }

    public DateTime? FechaDesde { get; init; }

    public DateTime? FechaHasta { get; init; }

    [Range(1, MaxPageSize)]
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }
}
