using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.EstadosCita;

public sealed record CreateEstadoCitaRequest(
    [Required, MaxLength(80)] string Nombre,
    [MaxLength(300)] string? Descripcion,
    bool EsEstadoFinal = false,
    bool Activo = true);
