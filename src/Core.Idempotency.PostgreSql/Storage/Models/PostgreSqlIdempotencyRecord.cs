namespace Core.Idempotency.PostgreSql.Storage.Models;

internal sealed class PostgreSqlIdempotencyRecord
{
    public string? RequestFingerprint { get; init; }

    public string? HashAlgorithm { get; init; }

    public int StatusCode { get; init; }

    public string? ContentType { get; init; }

    public byte[] Headers { get; init; } = [];

    public byte[] Body { get; init; } = [];
}
