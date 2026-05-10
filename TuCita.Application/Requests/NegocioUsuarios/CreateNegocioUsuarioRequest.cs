using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.NegocioUsuarios;

public sealed record CreateNegocioUsuarioRequest(
    [Required, MaxLength(128)] string UserId,
    [Required] int IdRolNegocio,
    bool Activo = true);
