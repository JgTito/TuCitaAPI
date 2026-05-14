namespace TuCita.Application.InformesInteligentes;

public sealed record InformeInteligenteIndicadoresDto(
    int TotalCitas,
    int CitasPendientes,
    int CitasConfirmadas,
    int CitasAtendidas,
    int CitasCanceladas,
    int CitasNoAsistidas,
    decimal TasaAsistencia,
    decimal TasaCancelacion,
    decimal TasaNoAsistencia,
    decimal TasaCancelacionNoAsistencia,
    decimal IngresosEstimados,
    decimal TicketPromedioEstimado,
    int ClientesUnicos,
    int ClientesNuevos,
    int ClientesRecurrentes,
    decimal TasaNoAsistenciaClientesNuevos,
    decimal TasaNoAsistenciaClientesRecurrentes,
    decimal HorasReservadas,
    decimal HorasDisponiblesEstimadas,
    decimal TasaOcupacionAgenda,
    decimal PromedioHorasEntreReservaYAtencion);
