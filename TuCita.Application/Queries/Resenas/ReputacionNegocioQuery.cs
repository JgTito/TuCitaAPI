using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Resenas;

public sealed class ReputacionNegocioQuery
{
    public DateTime? FechaDesde { get; init; }

    public DateTime? FechaHasta { get; init; }

    [Range(1, 60)]
    public int MesesEvolucion { get; init; } = 12;

    public bool IncluirNoPublicadas { get; init; }
}
