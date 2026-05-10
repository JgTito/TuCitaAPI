using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Servicios;

public sealed record CreateServicioRequest(
    int? IdCategoriaServicio,
    [Required, MaxLength(150)] string Nombre,
    [MaxLength(500)] string? Descripcion,
    [Range(1, int.MaxValue)] int DuracionMinutos,
    [Range(typeof(decimal), "0", "9999999999999999")] decimal Precio,
    bool RequiereProfesional = true,
    bool RequierePagoAnticipado = false,
    [Range(0, int.MaxValue)] int TiempoPreparacionMinutos = 0,
    bool Activo = true);
