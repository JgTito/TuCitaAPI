using Microsoft.AspNetCore.Identity;

namespace TuCita.Infrastucture.Entities;

public sealed class EstadoNotificacion
{
    public int IdEstadoNotificacion { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Notificacion> Notificaciones { get; set; } = [];
}

