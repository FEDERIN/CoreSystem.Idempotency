namespace Core.Idempotency.Redis.DependencyInjection;

internal static class RedisMessages
{
    public const string RedisConfigurationRequired =
    "Redis configuration is required when using the Redis idempotency provider.";

    public const string IdempotencyRegistrationRequired =
        "Idempotency options are required when using the Redis idempotency provider.";
}