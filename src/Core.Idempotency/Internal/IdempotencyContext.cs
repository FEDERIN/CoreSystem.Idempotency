namespace Core.Idempotency.Internal;

internal sealed class IdempotencyContext
{
    public required string Key { get; init; }

    public required TimeSpan Expiration { get; init; }
}