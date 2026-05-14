using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.InformesInteligentes;

public sealed class InformeInteligenteQuery
{
    private const int MaxTop = 50;
    private int _top = 10;

    public DateTime? FechaDesde { get; init; }

    public DateTime? FechaHasta { get; init; }

    public bool CompararPeriodoAnterior { get; init; } = true;

    public bool IncluirPromptSugerido { get; init; } = true;

    [Range(1, MaxTop)]
    public int Top
    {
        get => _top;
        init => _top = Math.Clamp(value, 1, MaxTop);
    }
}
