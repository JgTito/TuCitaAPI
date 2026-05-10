namespace TuCita.Infrastucture.Entities;

public sealed class Pais
{
    public int IdPais { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string CodigoIso2 { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public ICollection<Ciudad> Ciudades { get; set; } = [];
}
