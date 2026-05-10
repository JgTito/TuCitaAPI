using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Clientes;

public sealed class ClienteSelectQuery
{
    [MaxLength(150)]
    public string? Search { get; init; }

    [MaxLength(128)]
    public string? UserId { get; init; }

    public bool? TieneUsuario { get; init; }

    public bool SoloActivos { get; init; } = true;
}
