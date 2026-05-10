using System.Security.Cryptography;
using System.Text;

namespace TuCita.Infrastucture.Authentication;

public static class TokenHasher
{
    public static string Hash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
