using Microsoft.AspNetCore.Http;

namespace Core.Idempotency.Fingerprint;

internal interface IRequestFingerprintBuilder
{
    ValueTask<string> BuildAsync(
        HttpContext context,
        CancellationToken cancellationToken = default);
}