namespace TuCita.Application.Pagos;

public sealed record ProcesarPagosFlowPendientesResultDto(
    int Consultados,
    int Exitosos,
    int ConError);
