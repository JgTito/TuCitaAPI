using TuCita.Application.Common;
using TuCita.Application.InformesInteligentes;

namespace TuCita.Infrastucture.InformesInteligentes;

public interface IInformeInteligenteAiClient
{
    Task<ServiceResult<InformeAiGenerationResult>> GenerarInformeAsync(
        InformeInteligenteContextoDto contexto,
        CancellationToken cancellationToken);
}

public sealed record InformeAiGenerationResult(
    string Text,
    string Model);
