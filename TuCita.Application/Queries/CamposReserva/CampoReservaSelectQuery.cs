using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.CamposReserva;

public sealed class CampoReservaSelectQuery
{
    [MaxLength(150)]
    public string? Search { get; init; }

    public int? IdTipoCampo { get; init; }

    public int? IdServicio { get; init; }

    public bool? SoloGlobales { get; init; }

    public bool? Obligatorio { get; init; }

    public bool SoloActivos { get; init; } = true;
}
