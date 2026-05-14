namespace TuCita.Application.InformesInteligentes;

public sealed record InformeInteligenteComparacionDto(
    InformeInteligentePeriodoDto PeriodoAnterior,
    int TotalCitasAnterior,
    decimal VariacionTotalCitas,
    decimal IngresosEstimadosAnterior,
    decimal VariacionIngresosEstimados,
    decimal TasaAsistenciaAnterior,
    decimal VariacionTasaAsistencia,
    decimal TasaCancelacionNoAsistenciaAnterior,
    decimal VariacionTasaCancelacionNoAsistencia,
    decimal TasaOcupacionAnterior,
    decimal VariacionTasaOcupacion);
