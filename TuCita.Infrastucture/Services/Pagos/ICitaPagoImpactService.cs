using TuCita.Infrastucture.Entities;

namespace TuCita.Infrastucture.Pagos;

public interface ICitaPagoImpactService
{
    CitaPagoSnapshot Capture(Cita cita);

    Task RegistrarActualizacionAsync(
        Cita cita,
        CitaPagoSnapshot snapshot,
        string? userId,
        string motivo,
        CancellationToken cancellationToken);

    Task RegistrarReagendamientoAsync(
        Cita cita,
        CitaPagoSnapshot snapshot,
        string? userId,
        string motivo,
        CancellationToken cancellationToken);

    Task RegistrarCancelacionAsync(
        Cita cita,
        CitaPagoSnapshot snapshot,
        string? userId,
        string motivo,
        CancellationToken cancellationToken);
}

public sealed record CitaPagoSnapshot(
    int IdEstadoCita,
    int IdServicio,
    int? IdPrestador,
    DateTime FechaInicio,
    DateTime FechaFin,
    decimal PrecioEstimado);
