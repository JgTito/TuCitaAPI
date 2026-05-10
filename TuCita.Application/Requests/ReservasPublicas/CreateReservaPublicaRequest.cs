using System.ComponentModel.DataAnnotations;
using TuCita.Application.Common;
using TuCita.Application.Citas;

namespace TuCita.Application.ReservasPublicas;

public sealed record CreateReservaPublicaRequest(
    [Required] int IdServicio,
    int? IdPrestador,
    [Required] DateTime FechaInicio,
    [RequiredNonWhiteSpace, MaxLength(150)] string NombreCliente,
    [MaxLength(30)] string? Telefono,
    [EmailAddress, MaxLength(150)] string? Email,
    [MaxLength(20)] string? Rut,
    [MaxLength(1000)] string? ComentarioCliente,
    IReadOnlyCollection<CitaCampoValorRequest>? CamposValor);
