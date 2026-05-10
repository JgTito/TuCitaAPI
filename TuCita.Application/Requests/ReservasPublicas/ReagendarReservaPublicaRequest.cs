using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.ReservasPublicas;

public sealed record ReagendarReservaPublicaRequest(
    [EmailAddress, MaxLength(150)] string? Email,
    [MaxLength(30)] string? Telefono,
    [Required] DateTime FechaInicio,
    DateTime? FechaFin,
    int? IdPrestador,
    [MaxLength(1000)] string? Observacion);
