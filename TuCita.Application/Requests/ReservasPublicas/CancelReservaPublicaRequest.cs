using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.ReservasPublicas;

public sealed record CancelReservaPublicaRequest(
    [EmailAddress, MaxLength(150)] string? Email,
    [MaxLength(30)] string? Telefono,
    [MaxLength(1000)] string? Observacion);
