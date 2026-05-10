using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Citas;

public sealed record UpdateCitaRequest(
    [Required] int IdCliente,
    [Required] int IdServicio,
    int? IdPrestador,
    [Required] DateTime FechaInicio,
    DateTime? FechaFin,
    [MaxLength(1000)] string? ComentarioCliente,
    [MaxLength(1000)] string? NotaInterna,
    decimal? PrecioEstimado,
    IReadOnlyCollection<CitaCampoValorRequest>? CamposValor);
