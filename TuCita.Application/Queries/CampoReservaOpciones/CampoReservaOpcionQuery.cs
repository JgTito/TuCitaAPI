using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.CampoReservaOpciones;

public sealed class CampoReservaOpcionQuery
{
    private const int MaxPageSize = 100;
    private int _pageNumber = 1;
    private int _pageSize = 20;

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
