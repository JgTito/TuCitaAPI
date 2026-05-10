using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class CampoReservaOpcion
{
    public int IdCampoReservaOpcion { get; set; }
    public int IdNegocio { get; set; }
    public int IdCampoReserva { get; set; }
    public string Etiqueta { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;

    public CampoReserva CampoReserva { get; set; } = null!;
}

