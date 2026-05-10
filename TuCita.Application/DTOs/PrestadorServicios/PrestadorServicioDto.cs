namespace TuCita.Application.PrestadorServicios;

public sealed record PrestadorServicioDto(
    int IdPrestadorServicio,
    int IdNegocio,
    string Negocio,
    int IdPrestador,
    string Prestador,
    int IdServicio,
    string Servicio,
    int? IdCategoriaServicio,
    string? CategoriaServicio,
    int DuracionMinutos,
    decimal Precio,
    bool RequiereProfesional,
    bool RequierePagoAnticipado,
    bool Activo);
