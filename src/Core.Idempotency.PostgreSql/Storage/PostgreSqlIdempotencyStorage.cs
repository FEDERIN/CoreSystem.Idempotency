using Core.Idempotency.Abstractions;
using Core.Idempotency.Diagnostics;
using Core.Idempotency.Models;
using Core.Idempotency.PostgreSql.Options;
using Core.Idempotency.PostgreSql.Storage.Models;
using Core.Serialization.Abstractions;
using Dapper;
using Npgsql;
using System.Diagnostics;

namespace Core.Idempotency.PostgreSql.Storage;

internal sealed class PostgreSqlIdempotencyStorage(
    PostgreSqlOptions options,
    IPayloadSerializer serializer,
    IdempotencyMetrics metrics)
    : IIdempotencyStorage
{
    private const string Provider = "postgresql";
    private readonly string _connectionString =
        options.ConnectionString!;

    public async Task<IdempotencyEntry?> GetAsync(
        string key,
        CancellationToken ct = default)
    {
        const string sql = """
        SELECT
            request_fingerprint AS RequestFingerprint,
            hash_algorithm AS HashAlgorithm,
            status_code AS StatusCode,
            content_type AS ContentType,
            headers AS Headers,
            body AS Body
        FROM idempotency_keys
        WHERE key = @Key
          AND state = 'completed'
          AND expires_at > NOW();
        """;

        long start = Stopwatch.GetTimestamp();

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            var record = await conn.QueryFirstOrDefaultAsync<PostgreSqlIdempotencyRecord>(
                new CommandDefinition(
                    sql,
                    new { Key = key },
                    cancellationToken: ct));

            if (record is null)
            {
                return null;
            }

            if (record.RequestFingerprint is not null &&
                string.IsNullOrWhiteSpace(record.HashAlgorithm))
            {
                throw new InvalidOperationException(
                    "Stored fingerprint does not contain a hash algorithm.");
            }

            RequestFingerprint? fingerprint = null;

            if (record.RequestFingerprint is not null)
            {
                fingerprint = new RequestFingerprint
                {
                    HashAlgorithm = record.HashAlgorithm!,
                    Value = record.RequestFingerprint
                };
            }

            return new IdempotencyEntry
            {
                RequestFingerprint = fingerprint,
                Response = new IdempotencyResponse
                {
                    StatusCode = record.StatusCode,
                    ContentType = record.ContentType,
                    Body = record.Body,
                    Headers = serializer.Deserialize<Dictionary<string, string[]>>(record.Headers)
                              ?? []
                }
            };
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
        const string sql = """
        INSERT INTO idempotency_keys (key, state, lease_id, expires_at)
        VALUES (@Key, 'in_progress', @LeaseId, NOW() + @LeaseDuration)
        ON CONFLICT (key) DO UPDATE
        SET state = 'in_progress', lease_id = EXCLUDED.lease_id, expires_at = EXCLUDED.expires_at
        WHERE idempotency_keys.expires_at <= NOW()
        RETURNING state;
        """;

        var leaseId = Guid.NewGuid().ToString("N");
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var state = await conn.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
            sql, new { Key = key, LeaseId = leaseId, LeaseDuration = leaseDuration }, cancellationToken: ct));

        if (state == "in_progress")
        {
            return new IdempotencyAcquireResult
            {
                Status = IdempotencyAcquireStatus.Acquired,
                LeaseId = leaseId
            };
        }

        var entry = await GetAsync(key, ct);
        return entry is null
            ? new IdempotencyAcquireResult { Status = IdempotencyAcquireStatus.InProgress }
            : new IdempotencyAcquireResult { Status = IdempotencyAcquireStatus.Completed, Entry = entry };
    }

    public async Task<bool> CompleteAsync(
        string key,
        string leaseId,
        IdempotencyEntry entry,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        const string sql = """
        UPDATE idempotency_keys
        SET state = 'completed', lease_id = NULL,
            request_fingerprint = @RequestFingerprint,
            hash_algorithm = @HashAlgorithm,
            status_code = @StatusCode,
            content_type = @ContentType,
            headers = @Headers,
            body = @Body,
            expires_at = NOW() + @Expiration
        WHERE key = @Key AND state = 'in_progress' AND lease_id = @LeaseId;
        """;

        long start = Stopwatch.GetTimestamp();
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            var response = entry.Response;
            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                Key = key,
                LeaseId = leaseId,
                RequestFingerprint = entry.RequestFingerprint?.Value,
                entry.RequestFingerprint?.HashAlgorithm,
                response.StatusCode,
                response.ContentType,
                Headers = serializer.Serialize(response.Headers),
                response.Body,
                Expiration = expiration ?? TimeSpan.FromDays(1)
            }, cancellationToken: ct));

            if (rows > 0)
            {
                metrics.RecordStorageWrite(Provider);
                metrics.RecordPayloadSize(response.Body.LongLength, Provider);
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
        const string sql = "DELETE FROM idempotency_keys WHERE key = @Key AND state = 'in_progress' AND lease_id = @LeaseId;";
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Key = key, LeaseId = leaseId }, cancellationToken: ct));
    }

    public async Task SetAsync(
        string key,
        IdempotencyEntry entry,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        const string sql = """
        INSERT INTO idempotency_keys
        (
            key,
            request_Fingerprint,
            hash_Algorithm,
            status_code,
            content_type,
            headers,
            body,
            expires_at
        )
        VALUES
        (
            @Key,
            @RequestFingerprint,
            @HashAlgorithm,
            @StatusCode,
            @ContentType,
            @Headers,
            @Body,
            NOW() + @Expiration
        )
        ON CONFLICT (key)
        DO UPDATE
        SET
            request_fingerprint = EXCLUDED.request_fingerprint,
            hash_algorithm = EXCLUDED.hash_algorithm,
            status_code = EXCLUDED.status_code,
            content_type = EXCLUDED.content_type,
            headers = EXCLUDED.headers,
            body = EXCLUDED.body,
            expires_at = EXCLUDED.expires_at
        WHERE idempotency_keys.expires_at < NOW();
        """;

        var expiresIn = expiration ?? TimeSpan.FromDays(1);

        long start = Stopwatch.GetTimestamp();

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            var response = entry.Response;

            var rows = await conn.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        Key = key,
                        RequestFingerprint = entry.RequestFingerprint?.Value,
                        entry.RequestFingerprint?.HashAlgorithm,
                        response.StatusCode,
                        response.ContentType,
                        Headers = serializer.Serialize(response.Headers),
                        response.Body,
                        Expiration = expiresIn
                    },
                    cancellationToken: ct));

            if (rows > 0)
            {
                metrics.RecordStorageWrite(Provider);

                metrics.RecordPayloadSize(response.Body.LongLength, Provider);
            }
        }
        finally
        {
            metrics.RecordStorageWriteDuration(Stopwatch.GetElapsedTime(start).TotalMilliseconds, Provider);
        }
    }
}
