using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class EstadoCita
{
    public int IdEstadoCita { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsEstadoFinal { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Cita> Citas { get; set; } = [];
    public ICollection<CitaHistorial> HistorialesEstadoAnterior { get; set; } = [];
    public ICollection<CitaHistorial> HistorialesEstadoNuevo { get; set; } = [];
}

