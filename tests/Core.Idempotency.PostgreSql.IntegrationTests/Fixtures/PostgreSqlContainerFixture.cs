using Dapper;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Core.Idempotency.PostgreSql.IntegrationTests.Fixtures;

public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    private const string PostgreSqlImage = "postgres:17-alpine";

    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder(PostgreSqlImage)
            .WithDatabase("idempotency")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();


    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await InitializeDatabaseAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);

        await connection.OpenAsync();

        var scriptPath = Path.Combine(
            AppContext.BaseDirectory,
            "Scripts",
            "postgresql.sql");

        var sql = await File.ReadAllTextAsync(scriptPath);

        await connection.ExecuteAsync(sql);
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}