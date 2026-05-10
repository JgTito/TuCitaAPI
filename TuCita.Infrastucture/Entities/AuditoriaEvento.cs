using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class AuditoriaEvento
{
    public int IdAuditoriaEvento { get; set; }
    public int? IdNegocio { get; set; }
    public string? UserId { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public string? EntidadId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? ValoresAnterioresJson { get; set; }
    public string? ValoresNuevosJson { get; set; }
    public string? CambiosJson { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Negocio? Negocio { get; set; }
    public IdentityUser? Usuario { get; set; }
}
