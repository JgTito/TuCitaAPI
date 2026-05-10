using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Resenas;

public sealed record UpdateConfiguracionResenaNegocioRequest(
    bool ResenasActivas,
    bool AutoaprobarResenas,
    [Range(1, 365)] int DiasMaximosParaCalificar,
    [Range(1, 5)] byte PuntuacionMaximaAlertaOperativa,
    bool PermitirRespuestaNegocio,
    bool MostrarResenasPublicas);
