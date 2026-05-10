using TuCita.Application.Auth;
using TuCita.Application.Negocios;

namespace TuCita.Application.Onboarding;

public sealed record OnboardingNegocioResponse(
    AuthResponse Auth,
    NegocioDto Negocio);
