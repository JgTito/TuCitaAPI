namespace TuCita.Infrastucture.Entities;

public sealed class SolicitudResena
{
    public int IdSolicitudResena { get; set; }
    public int IdNegocio { get; set; }
    public int IdCita { get; set; }
    public int IdCliente { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string Estado { get; set; } = SolicitudResenaEstados.Pendiente;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public DateTime? FechaUso { get; set; }
    public DateTime? FechaCancelacion { get; set; }

    public Negocio Negocio { get; set; } = null!;
    public Cita Cita { get; set; } = null!;
    public Cliente Cliente { get; set; } = null!;
}

public static class SolicitudResenaEstados
{
    public const string Pendiente = "Pendiente";
    public const string Usada = "Usada";
    public const string Expirada = "Expirada";
    public const string Cancelada = "Cancelada";
}
