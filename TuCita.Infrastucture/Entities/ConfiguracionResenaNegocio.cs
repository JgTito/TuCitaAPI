namespace TuCita.Infrastucture.Entities;

public sealed class ConfiguracionResenaNegocio
{
    public int IdConfiguracionResenaNegocio { get; set; }
    public int IdNegocio { get; set; }
    public bool ResenasActivas { get; set; } = true;
    public bool AutoaprobarResenas { get; set; }
    public int DiasMaximosParaCalificar { get; set; } = 15;
    public byte PuntuacionMaximaAlertaOperativa { get; set; } = 2;
    public bool PermitirRespuestaNegocio { get; set; } = true;
    public bool MostrarResenasPublicas { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }

    public Negocio Negocio { get; set; } = null!;
}
