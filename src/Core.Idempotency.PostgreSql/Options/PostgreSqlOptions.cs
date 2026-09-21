namespace Core.Idempotency.PostgreSql.Options;

/// <summary>
/// Configuration options for the PostgreSQL provider.
/// </summary>
public sealed class PostgreSqlOptions
{
    /// <summary>
    /// Connection string used by the PostgreSQL provider.
    /// </summary>
    public string? ConnectionString { get; set; }
}