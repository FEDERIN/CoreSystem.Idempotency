using Core.Idempotency.Models;

namespace Core.Idempotency.Abstractions;

public interface IIdempotencyStorage
{
    Task<IdempotencyAcquireResult> TryAcquireAsync(
        string key,
        TimeSpan leaseDuration,
        CancellationToken ct = default);

    Task<bool> CompleteAsync(
        string key,
        string leaseId,
        IdempotencyEntry entry,
        TimeSpan? expiration = null,
        CancellationToken ct = default);

    Task ReleaseAsync(
        string key,
        string leaseId,
        CancellationToken ct = default);

    Task<IdempotencyEntry?> GetAsync(
        string key,
        CancellationToken ct = default);

    Task SetAsync(
        string key,
        IdempotencyEntry entry,
        TimeSpan? expiration = null,
        CancellationToken ct = default);
}
