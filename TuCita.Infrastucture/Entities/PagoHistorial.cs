using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class PagoHistorial
{
    public int IdPagoHistorial { get; set; }
    public int IdPago { get; set; }
    public int IdNegocio { get; set; }
    public int IdCita { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    public string? EstadoAnterior { get; set; }
    public string? EstadoNuevo { get; set; }
    public decimal? Monto { get; set; }
    public string? Motivo { get; set; }
    public string? Referencia { get; set; }
    public string? UserId { get; set; }
    public string? DatosJson { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Pago Pago { get; set; } = null!;
    public Negocio Negocio { get; set; } = null!;
    public Cita Cita { get; set; } = null!;
    public IdentityUser? Usuario { get; set; }
}
