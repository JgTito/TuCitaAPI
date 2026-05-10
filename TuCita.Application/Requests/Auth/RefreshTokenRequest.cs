using System.ComponentModel.DataAnnotations;
using TuCita.Application.Common;

namespace TuCita.Application.Auth;

public sealed record RefreshTokenRequest(
    [RequiredNonWhiteSpace] string RefreshToken);
