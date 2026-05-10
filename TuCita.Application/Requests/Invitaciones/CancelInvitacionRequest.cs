using System.ComponentModel.DataAnnotations;

namespace TuCita.Application.Invitaciones;

public sealed record CancelInvitacionRequest(
    [MaxLength(300)] string? Motivo);
