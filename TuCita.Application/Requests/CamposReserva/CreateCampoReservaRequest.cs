using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.CamposReserva;

public sealed record CreateCampoReservaRequest(
    int? IdServicio,
    [Required] int IdTipoCampo,
    [Required, MaxLength(100)] string NombreInterno,
    [Required, MaxLength(150)] string Etiqueta,
    [MaxLength(150)] string? Placeholder,
    [MaxLength(300)] string? TextoAyuda,
    bool Obligatorio = false,
    [Range(0, int.MaxValue)] int Orden = 0,
    bool Activo = true);
