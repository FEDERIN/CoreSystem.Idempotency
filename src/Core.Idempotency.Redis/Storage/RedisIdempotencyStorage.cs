using Core.Idempotency.Diagnostics;
using Core.Idempotency.Models;
using Core.Idempotency.Abstractions;
using Core.Serialization.Abstractions;
using StackExchange.Redis;
using System.Diagnostics;
using Core.Idempotency.Redis.Builders;


namespace Core.Idempotency.Redis.Storage;

internal sealed class RedisIdempotencyStorage(
    IConnectionMultiplexer redis,
    IKeyBuilder keyBuilder,
    IPayloadSerializer payloadSerializer,
    IdempotencyMetrics metrics)
    : IIdempotencyStorage
{
    private const string Provider = "redis";
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task<IdempotencyEntry?> GetAsync(
        string key,
        CancellationToken ct = default)
    {
        var redisKey = keyBuilder.BuildCacheKey(key);

        long start = Stopwatch.GetTimestamp();

        try
        {
            var serializedEntry = await _database.StringGetAsync(redisKey);

            if (serializedEntry.IsNullOrEmpty)
                return null;

            return payloadSerializer.Deserialize<IdempotencyEntry>(serializedEntry);
        }
        finally
        {
            metrics.RecordStorageReadDuration(Stopwatch.GetElapsedTime(start).TotalMilliseconds, Provider);
        }
    }

    public async Task<IdempotencyAcquireResult> TryAcquireAsync(
        string key,
        TimeSpan leaseDuration,
        CancellationToken ct = default)
    {
        var existing = await GetAsync(key, ct);
        if (existing is not null)
        {
            return new IdempotencyAcquireResult
            {
                Status = IdempotencyAcquireStatus.Completed,
                Entry = existing
            };
        }

        var leaseId = Guid.NewGuid().ToString("N");
        var acquired = await _database.StringSetAsync(
            keyBuilder.BuildLock(key), leaseId, leaseDuration, When.NotExists);

        return new IdempotencyAcquireResult
        {
            Status = acquired
                ? IdempotencyAcquireStatus.Acquired
                : IdempotencyAcquireStatus.InProgress,
            LeaseId = acquired ? leaseId : null
        };
    }

    public async Task<bool> CompleteAsync(
        string key,
        string leaseId,
        IdempotencyEntry entry,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        const string script = """
            if redis.call('GET', KEYS[2]) ~= ARGV[1] then return 0 end
            if redis.call('EXISTS', KEYS[1]) == 1 then redis.call('DEL', KEYS[2]); return 0 end
            redis.call('SET', KEYS[1], ARGV[2], 'PX', ARGV[3])
            redis.call('DEL', KEYS[2])
            return 1
            """;

        var payload = payloadSerializer.Serialize(entry);
        var expiry = expiration ?? TimeSpan.FromDays(1);
        long start = Stopwatch.GetTimestamp();

        try
        {
            var result = (int)await _database.ScriptEvaluateAsync(
                script,
                [keyBuilder.BuildCacheKey(key), keyBuilder.BuildLock(key)],
                [leaseId, payload, (long)expiry.TotalMilliseconds]);

            if (result == 1)
            {
                metrics.RecordStorageWrite(Provider);
                metrics.RecordPayloadSize(entry.Response.Body.LongLength, Provider);
                return true;
            }

            return false;
        }
        finally
        {
            metrics.RecordStorageWriteDuration(Stopwatch.GetElapsedTime(start).TotalMilliseconds, Provider);
        }
    }

    public async Task ReleaseAsync(string key, string leaseId, CancellationToken ct = default)
    {
        const string script = """
            if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) end
            return 0
            """;

        await _database.ScriptEvaluateAsync(script, [keyBuilder.BuildLock(key)], [leaseId]);
    }

    public async Task SetAsync(
        string key,
        IdempotencyEntry entry,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        var redisKey = keyBuilder.BuildCacheKey(key);
        var payload = payloadSerializer.Serialize(entry);

        Expiration expiry = expiration.HasValue
            ? new Expiration(expiration.Value)
            : default;

        long start = Stopwatch.GetTimestamp();

        try
        {
            if (await _database.StringSetAsync(redisKey, payload, expiry, When.NotExists))
            {
                metrics.RecordStorageWrite(Provider);
                metrics.RecordPayloadSize(entry.Response.Body.LongLength, Provider);
            }
        }
        finally
        {
            metrics.RecordStorageWriteDuration(Stopwatch.GetElapsedTime(start).TotalMilliseconds, Provider);
        }
    }
}
