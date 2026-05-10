using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.CampoReservaOpciones;

public sealed record CreateCampoReservaOpcionRequest(
    [Required, MaxLength(150)] string Etiqueta,
    [Required, MaxLength(150)] string Valor,
    [Range(0, int.MaxValue)] int Orden = 0,
    bool Activo = true);
