using TuCita.Application.Common;

namespace TuCita.Application.Onboarding;

public interface IOnboardingService
{
    Task<ServiceResult<OnboardingNegocioResponse>> RegisterDuenoNegocioAsync(
        RegisterDuenoNegocioRequest request,
        string? avatarUrl,
        CancellationToken cancellationToken);
}
