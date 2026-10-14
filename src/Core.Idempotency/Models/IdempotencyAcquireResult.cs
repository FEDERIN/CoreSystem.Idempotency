namespace Core.Idempotency.Models;

/// <summary>
/// Outcome of an attempt to claim an idempotency key.
/// </summary>
public enum IdempotencyAcquireStatus
{
    /// <summary>
    /// The caller acquired the lease and is responsible for executing the request
    /// and then completing or releasing the key.
    /// </summary>
    Acquired,

    /// <summary>
    /// Another request currently owns the lease. The caller must not execute the
    /// endpoint and should reject the request with a conflict response.
    /// </summary>
    InProgress,

    /// <summary>
    /// A completed entry already exists. The caller should replay
    /// <see cref="IdempotencyAcquireResult.Entry"/> without executing the endpoint.
    /// </summary>
    Completed
}

/// <summary>
/// Result of an attempt to claim an idempotency key.
/// </summary>
public sealed class IdempotencyAcquireResult
{
    /// <summary>
    /// Gets the outcome of the attempt.
    /// </summary>
    public required IdempotencyAcquireStatus Status { get; init; }

    /// <summary>
    /// Gets the lease identifier issued to the caller.
    /// Required when <see cref="Status"/> is
    /// <see cref="IdempotencyAcquireStatus.Acquired"/>, and used to complete or
    /// release the entry.
    /// </summary>
    public string? LeaseId { get; init; }

    /// <summary>
    /// Gets the stored entry to replay.
    /// Populated when <see cref="Status"/> is
    /// <see cref="IdempotencyAcquireStatus.Completed"/>.
    /// </summary>
    public IdempotencyEntry? Entry { get; init; }
}