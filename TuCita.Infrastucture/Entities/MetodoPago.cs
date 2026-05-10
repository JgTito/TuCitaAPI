namespace TuCita.Infrastucture.Entities;

public sealed class MetodoPago
{
    public int IdMetodoPago { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsManual { get; set; }
    public bool EsOnline { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Pago> Pagos { get; set; } = [];
}
