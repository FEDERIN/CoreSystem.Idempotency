namespace Core.Idempotency.Models;

public enum IdempotencyAcquireStatus
{
    Acquired,
    InProgress,
    Completed
}

public sealed class IdempotencyAcquireResult
{
    public required IdempotencyAcquireStatus Status { get; init; }

    public string? LeaseId { get; init; }

    public IdempotencyEntry? Entry { get; init; }
}
