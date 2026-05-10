using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Resenas;

public sealed class ResenaPublicaQuery
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 50)]
    public int PageSize { get; init; } = 10;

    [Range(1, 5)]
    public byte? Puntuacion { get; init; }

    public int? IdServicio { get; init; }

    public int? IdPrestador { get; init; }
}
