namespace TuCita.Infrastucture.Entities;

public sealed class EstadoPago
{
    public int IdEstadoPago { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsEstadoFinal { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Pago> Pagos { get; set; } = [];
}
