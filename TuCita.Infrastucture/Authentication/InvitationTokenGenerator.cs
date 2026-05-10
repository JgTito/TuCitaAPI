using System.Security.Cryptography;

namespace TuCita.Infrastucture.Authentication;

public static class InvitationTokenGenerator
{
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
