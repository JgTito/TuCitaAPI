using System.ComponentModel.DataAnnotations;
using TuCita.Application.Common;

namespace TuCita.Application.Auth;

public sealed record LoginRequest(
    [RequiredNonWhiteSpace, EmailAddress] string Email,
    [RequiredNonWhiteSpace] string Password);
