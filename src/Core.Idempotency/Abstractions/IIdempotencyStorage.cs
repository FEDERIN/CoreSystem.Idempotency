using Core.Idempotency.Models;

namespace Core.Idempotency.Abstractions;

/// <summary>
/// Persistence contract for idempotency entries.
/// </summary>
/// <remarks>
/// Implementations live in separate provider packages. Adding a new persistence
/// technology requires a new implementation of this interface; the core package
/// does not change.
/// <para>
/// The lifecycle is lease based. A request first claims a key with
/// <see cref="TryAcquireAsync"/>, then either stores its response with
/// <see cref="CompleteAsync"/> or hands the key back with <see cref="ReleaseAsync"/>.
/// Only the holder of a lease may complete or release the entry.
/// </para>
/// <para>
/// <see cref="TryAcquireAsync"/> must be atomic. Two concurrent requests presenting
/// the same key must never both succeed; one acquires the lease and the other
/// observes <see cref="IdempotencyAcquireStatus.InProgress"/>.
/// </para>
/// </remarks>
public interface IIdempotencyStorage
{
    /// <summary>
    /// Attempts to claim ownership of an idempotency key.
    /// </summary>
    /// <param name="key">Fully qualified storage key, including any provider prefix.</param>
    /// <param name="leaseDuration">
    /// How long the lease remains valid. Once it expires another request may claim
    /// the key, so this bounds how long a crashed in-flight request can block others.
    /// </param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>
    /// The outcome of the attempt. <see cref="IdempotencyAcquireStatus.Acquired"/> when
    /// the caller now owns the lease, otherwise the current state of the entry.
    /// </returns>
    Task<IdempotencyAcquireResult> TryAcquireAsync(
        string key,
        TimeSpan leaseDuration,
        CancellationToken ct = default);

    /// <summary>
    /// Stores the response for a key previously claimed by <see cref="TryAcquireAsync"/>.
    /// </summary>
    /// <param name="key">Fully qualified storage key.</param>
    /// <param name="leaseId">Lease returned when the key was acquired.</param>
    /// <param name="entry">The fingerprint and response to persist.</param>
    /// <param name="expiration">
    /// How long the entry is retained. When <see langword="null"/>, the provider
    /// applies its own default.
    /// </param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> when the entry was written; <see langword="false"/> when
    /// the lease was lost or had already expired, in which case nothing was stored.
    /// </returns>
    Task<bool> CompleteAsync(
        string key,
        string leaseId,
        IdempotencyEntry entry,
        TimeSpan? expiration = null,
        CancellationToken ct = default);

    /// <summary>
    /// Releases a claimed key without storing a response, so the next request can retry it.
    /// </summary>
    /// <param name="key">Fully qualified storage key.</param>
    /// <param name="leaseId">Lease returned when the key was acquired.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the key has been released.</returns>
    Task ReleaseAsync(
        string key,
        string leaseId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a completed entry for replay.
    /// </summary>
    /// <param name="key">Fully qualified storage key.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>
    /// The stored entry, or <see langword="null"/> when no completed, unexpired entry
    /// exists for the key.
    /// </returns>
    Task<IdempotencyEntry?> GetAsync(
        string key,
        CancellationToken ct = default);

    /// <summary>
    /// Stores an entry without acquiring a lease first.
    /// </summary>
    /// <param name="key">Fully qualified storage key.</param>
    /// <param name="entry">The fingerprint and response to persist.</param>
    /// <param name="expiration">
    /// How long the entry is retained. When <see langword="null"/>, the provider
    /// applies its own default.
    /// </param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the entry has been stored.</returns>
    Task SetAsync(
        string key,
        IdempotencyEntry entry,
        TimeSpan? expiration = null,
        CancellationToken ct = default);
}