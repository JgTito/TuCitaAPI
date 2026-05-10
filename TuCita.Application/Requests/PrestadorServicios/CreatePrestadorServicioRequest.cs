using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.PrestadorServicios;

public sealed record CreatePrestadorServicioRequest(
    [Required] int IdServicio,
    bool Activo = true);
