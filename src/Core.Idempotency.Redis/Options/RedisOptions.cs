using StackExchange.Redis;

namespace Core.Idempotency.Redis.Options;

/// <summary>
/// Configuration options for the Redis provider.
/// </summary>
public sealed class RedisOptions
{
    /// <summary>
    /// Delegate used to configure the Redis connection.
    /// </summary>
    public Action<ConfigurationOptions>? Configuration { get; set; }
}