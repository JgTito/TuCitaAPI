using System.ComponentModel.DataAnnotations;
using TuCita.Application.Common;

namespace TuCita.Application.Clientes;

public sealed record ResolveClienteReservaRequest(
    [Range(1, int.MaxValue)] int IdNegocio,
    [RequiredNonWhiteSpace, MaxLength(150)] string Nombre,
    [EmailAddress, MaxLength(150)] string? Email,
    [MaxLength(30)] string? Telefono,
    [MaxLength(20)] string? Rut);
