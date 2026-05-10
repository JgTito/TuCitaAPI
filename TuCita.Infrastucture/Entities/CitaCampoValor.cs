using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class CitaCampoValor
{
    public int IdCitaCampoValor { get; set; }
    public int IdNegocio { get; set; }
    public int IdCita { get; set; }
    public int IdCampoReserva { get; set; }
    public string? Valor { get; set; }

    public Cita Cita { get; set; } = null!;
    public CampoReserva CampoReserva { get; set; } = null!;
}

