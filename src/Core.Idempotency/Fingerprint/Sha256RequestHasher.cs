using System.Security.Cryptography;
using System.Text;

namespace Core.Idempotency.Fingerprint;

internal sealed class Sha256RequestHasher : IRequestHasher
{
    public string Name => "SHA256";

    public string Compute(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(input));

        return Convert.ToHexString(hash);
    }
}