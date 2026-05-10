namespace TuCita.Application.Servicios;

public sealed record ServicioDto(
    int IdServicio,
    int IdNegocio,
    string Negocio,
    int? IdCategoriaServicio,
    string? CategoriaServicio,
    string Nombre,
    string? Descripcion,
    int DuracionMinutos,
    decimal Precio,
    bool RequiereProfesional,
    bool RequierePagoAnticipado,
    int TiempoPreparacionMinutos,
    bool Activo,
    DateTime FechaCreacion);
