using System.ComponentModel.DataAnnotations;
using TuCita.Application.Common;

namespace TuCita.Application.Invitaciones;

public sealed record AcceptInvitacionRequest(
    [RequiredNonWhiteSpace] string Token);
