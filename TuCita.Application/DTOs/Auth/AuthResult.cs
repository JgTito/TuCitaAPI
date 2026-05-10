namespace TuCita.Application.Auth;

public sealed record AuthResult(
    bool Succeeded,
    AuthResponse? Data,
    IReadOnlyCollection<string> Errors)
{
    public static AuthResult Success(AuthResponse data) => new(true, data, []);

    public static AuthResult Failure(IEnumerable<string> errors) => new(false, null, errors.ToArray());
}
