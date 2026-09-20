namespace Core.Idempotency.PostgreSql.DependencyInjection;

internal static class PostgreSqlMessages
{
    public const string PostgreSqlConnectionStringRequired =
        "PostgreSQL connection string is required.";

    public const string IdempotencyRegistrationRequired =
        "Idempotency options are required when using the PostgreSQL idempotency provider.";
}
