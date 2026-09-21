namespace Core.Idempotency.Redis.Builders;

internal sealed class RedisKeyBuilder(string prefix) : IKeyBuilder
{
    public string BuildCacheKey(string key)
        => $"{prefix}Idempotency:{key}";

    public string BuildLock(string key)
        => $"{BuildCacheKey(key)}:lock";
}
