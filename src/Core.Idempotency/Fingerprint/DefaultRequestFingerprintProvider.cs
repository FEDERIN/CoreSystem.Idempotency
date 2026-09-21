using Core.Idempotency.Models;
using Microsoft.AspNetCore.Http;

namespace Core.Idempotency.Fingerprint;

internal sealed class DefaultRequestFingerprintProvider(
    IRequestFingerprintBuilder builder,
    IRequestHasher hasher)
    : IRequestFingerprintProvider
{
    public async ValueTask<RequestFingerprint> ComputeAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        var payload = await builder.BuildAsync(
            context,
            cancellationToken);

        return new RequestFingerprint
        {
            HashAlgorithm = hasher.Name,
            Value = hasher.Compute(payload)
        };
    }
}