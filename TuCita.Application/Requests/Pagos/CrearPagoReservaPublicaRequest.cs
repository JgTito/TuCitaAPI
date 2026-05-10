using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Pagos;

public sealed record CrearPagoReservaPublicaRequest(
    [EmailAddress, MaxLength(150)] string? Email,
    [MaxLength(30)] string? Telefono);
