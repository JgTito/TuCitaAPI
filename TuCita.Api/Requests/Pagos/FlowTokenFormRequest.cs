using TuCita.Application.Common;

namespace TuCita.Api.Requests.Pagos;

public sealed class FlowTokenFormRequest
{
    [RequiredNonWhiteSpace]
    public string Token { get; init; } = string.Empty;
}
