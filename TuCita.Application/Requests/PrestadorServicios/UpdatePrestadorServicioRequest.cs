using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.PrestadorServicios;

public sealed record UpdatePrestadorServicioRequest(
    [Required] int IdServicio,
    bool Activo);
