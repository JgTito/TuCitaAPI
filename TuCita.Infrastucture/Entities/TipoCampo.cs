using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class TipoCampo
{
    public int IdTipoCampo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<CampoReserva> CamposReserva { get; set; } = [];
}

