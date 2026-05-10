using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Citas;

public sealed record CitaCampoValorRequest(
    [Required] int IdCampoReserva,
    string? Valor);
