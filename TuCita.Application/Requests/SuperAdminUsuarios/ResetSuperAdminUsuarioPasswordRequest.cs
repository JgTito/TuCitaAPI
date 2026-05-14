using System.ComponentModel.DataAnnotations;
using TuCita.Application.Common;

namespace TuCita.Application.SuperAdminUsuarios;

public sealed record ResetSuperAdminUsuarioPasswordRequest(
    [RequiredNonWhiteSpace, MinLength(6), MaxLength(100)] string NewPassword,
    [RequiredNonWhiteSpace, MaxLength(100)] string ConfirmPassword,
    bool ForzarCambioPassword = true);
