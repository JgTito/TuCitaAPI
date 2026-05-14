using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.SuperAdminUsuarios;

public sealed class SuperAdminRolSelectQuery
{
    [MaxLength(256)]
    public string? Search { get; init; }
}
