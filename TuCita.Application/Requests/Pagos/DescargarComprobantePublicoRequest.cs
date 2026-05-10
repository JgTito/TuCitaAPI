using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Pagos;

public sealed class DescargarComprobantePublicoRequest
{
    [EmailAddress]
    [MaxLength(150)]
    public string? Email { get; init; }

    [MaxLength(30)]
    public string? Telefono { get; init; }
}
