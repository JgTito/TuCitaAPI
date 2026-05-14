namespace TuCita.Application.InformesInteligentes;

public sealed record InformeInteligenteContextoDto(
    InformeInteligenteNegocioDto Negocio,
    InformeInteligentePeriodoDto Periodo,
    InformeInteligenteIndicadoresDto Indicadores,
    InformeInteligenteComparacionDto? ComparacionPeriodoAnterior,
    IReadOnlyCollection<InformeInteligenteEstadoDto> CitasPorEstado,
    IReadOnlyCollection<InformeInteligenteServicioDto> ServiciosMasSolicitados,
    IReadOnlyCollection<InformeInteligenteServicioDto> ServiciosMenorDemanda,
    IReadOnlyCollection<InformeInteligenteServicioDto> IngresosPorServicio,
    IReadOnlyCollection<InformeInteligentePrestadorDto> PrestadoresMayorCarga,
    IReadOnlyCollection<InformeInteligentePrestadorDto> PrestadoresMenorOcupacion,
    IReadOnlyCollection<InformeInteligenteHorarioDto> HorariosMayorDemanda,
    IReadOnlyCollection<InformeInteligenteHorarioDto> HorariosMenorDemanda,
    IReadOnlyCollection<InformeInteligenteDiaDto> DiasConMasReservas,
    IReadOnlyCollection<InformeInteligenteDiaDto> DiasConMasCancelaciones,
    IReadOnlyCollection<InformeInteligenteClienteSegmentoDto> ClientesNuevosVsRecurrentes,
    InformeInteligenteCalidadDatosDto CalidadDatos,
    string? PromptSugerido);
