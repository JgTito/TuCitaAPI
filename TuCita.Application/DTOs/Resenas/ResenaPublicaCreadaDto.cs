namespace TuCita.Application.Resenas;

public sealed record ResenaPublicaCreadaDto(
    int IdResenaNegocio,
    string Estado,
    bool QuedaPendienteModeracion,
    string Mensaje);
