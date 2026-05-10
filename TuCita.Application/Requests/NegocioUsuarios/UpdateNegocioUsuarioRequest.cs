using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.NegocioUsuarios;

public sealed record UpdateNegocioUsuarioRequest(
    [Required] int IdRolNegocio,
    bool Activo);
