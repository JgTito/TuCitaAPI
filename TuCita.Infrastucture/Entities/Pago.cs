using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class Pago
{
    public int IdPago { get; set; }
    public int IdNegocio { get; set; }
    public int IdCita { get; set; }
    public int IdEstadoPago { get; set; }
    public int IdMetodoPago { get; set; }
    public string? RegistradoPorUserId { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public bool EsManual { get; set; }
    public string CommerceOrder { get; set; } = string.Empty;
    public long? FlowOrder { get; set; }
    public string? Token { get; set; }
    public string? CheckoutUrl { get; set; }
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "CLP";
    public string? Subject { get; set; }
    public string? PayerEmail { get; set; }
    public int? PaymentMethod { get; set; }
    public int? FlowStatus { get; set; }
    public string? FlowStatusNombre { get; set; }
    public string? PaymentDataJson { get; set; }
    public string? RawCreateResponseJson { get; set; }
    public string? RawStatusResponseJson { get; set; }
    public string? ReferenciaManual { get; set; }
    public string? ObservacionManual { get; set; }
    public decimal MontoDevuelto { get; set; }
    public string? MotivoAnulacion { get; set; }
    public string? ReferenciaAnulacion { get; set; }
    public string? AnuladoPorUserId { get; set; }
    public string? Error { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public DateTime? FechaPago { get; set; }
    public DateTime? FechaRegistroManual { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public DateTime? FechaUltimaDevolucion { get; set; }
    public DateTime? FechaExpiracion { get; set; }
    public DateTime? FechaUltimaConsulta { get; set; }

    public Negocio Negocio { get; set; } = null!;
    public Cita Cita { get; set; } = null!;
    public EstadoPago EstadoPago { get; set; } = null!;
    public MetodoPago MetodoPago { get; set; } = null!;
    public IdentityUser? RegistradoPor { get; set; }
    public IdentityUser? AnuladoPor { get; set; }
    public ICollection<PagoHistorial> Historial { get; set; } = [];
}
