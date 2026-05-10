namespace TuCita.Application.Resenas;

public sealed record ConfiguracionResenaNegocioDto(
    int IdConfiguracionResenaNegocio,
    int IdNegocio,
    string Negocio,
    bool ResenasActivas,
    bool AutoaprobarResenas,
    int DiasMaximosParaCalificar,
    byte PuntuacionMaximaAlertaOperativa,
    bool PermitirRespuestaNegocio,
    bool MostrarResenasPublicas,
    DateTime FechaCreacion,
    DateTime FechaActualizacion);
