using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class CampoReserva
{
    public int IdCampoReserva { get; set; }
    public int IdNegocio { get; set; }
    public int? IdServicio { get; set; }
    public int IdTipoCampo { get; set; }
    public string NombreInterno { get; set; } = string.Empty;
    public string Etiqueta { get; set; } = string.Empty;
    public string? Placeholder { get; set; }
    public string? TextoAyuda { get; set; }
    public bool Obligatorio { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;

    public Negocio Negocio { get; set; } = null!;
    public Servicio? Servicio { get; set; }
    public TipoCampo TipoCampo { get; set; } = null!;
    public ICollection<CampoReservaOpcion> Opciones { get; set; } = [];
    public ICollection<CitaCampoValor> Valores { get; set; } = [];
}

