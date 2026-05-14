using TuCita.Application.Common;
using TuCita.Application.InformesInteligentes;

namespace TuCita.Infrastucture.InformesInteligentes;

internal interface IInformeInteligenteAiClient
{
    Task<ServiceResult<InformeAiGenerationResult>> GenerarInformeAsync(
        InformeInteligenteContextoDto contexto,
        CancellationToken cancellationToken);
}

internal sealed record InformeAiGenerationResult(
    string Text,
    string Model);
