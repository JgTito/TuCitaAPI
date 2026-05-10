using System.Security.Cryptography;
using System.Text;

namespace TuCita.Infrastucture.Pagos;

internal static class FlowSignature
{
    public static string Sign(IReadOnlyDictionary<string, string> parameters, string secretKey)
    {
        var toSign = string.Concat(
            parameters
                .Where(parameter => !string.Equals(parameter.Key, "s", StringComparison.Ordinal))
                .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                .Select(parameter => $"{parameter.Key}{parameter.Value}"));

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var valueBytes = Encoding.UTF8.GetBytes(toSign);
        var hash = HMACSHA256.HashData(keyBytes, valueBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
