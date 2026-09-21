namespace Core.Idempotency.Models;

public sealed class IdempotencyEntry
{
    public RequestFingerprint? RequestFingerprint { get; init; }

    public required IdempotencyResponse Response { get; init; }
}