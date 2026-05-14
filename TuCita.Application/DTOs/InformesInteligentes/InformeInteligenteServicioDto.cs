namespace TuCita.Application.InformesInteligentes;

public sealed record InformeInteligenteServicioDto(
    int IdServicio,
    string Servicio,
    int TotalCitas,
    decimal PorcentajeReservas,
    int CitasAtendidas,
    int CitasCanceladas,
    int CitasNoAsistidas,
    decimal TasaNoAsistencia,
    decimal IngresosEstimados,
    decimal TicketPromedioEstimado,
    decimal DuracionPromedioMinutos);
