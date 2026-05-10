using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.CamposReserva;

public sealed record UpdateCampoReservaRequest(
    int? IdServicio,
    [Required] int IdTipoCampo,
    [Required, MaxLength(100)] string NombreInterno,
    [Required, MaxLength(150)] string Etiqueta,
    [MaxLength(150)] string? Placeholder,
    [MaxLength(300)] string? TextoAyuda,
    bool Obligatorio,
    [Range(0, int.MaxValue)] int Orden,
    bool Activo);
