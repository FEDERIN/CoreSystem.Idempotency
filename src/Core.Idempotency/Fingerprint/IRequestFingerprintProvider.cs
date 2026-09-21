using Core.Idempotency.Models;
using Microsoft.AspNetCore.Http;

namespace Core.Idempotency.Fingerprint;

internal interface IRequestFingerprintProvider
{
    ValueTask<RequestFingerprint> ComputeAsync(
        HttpContext context,
        CancellationToken cancellationToken = default);
}