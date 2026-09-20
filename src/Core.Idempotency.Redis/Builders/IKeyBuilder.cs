namespace Core.Idempotency.Redis.Builders;

internal interface IKeyBuilder
{
    string BuildCacheKey(string key);
    string BuildLock(string key);
}
