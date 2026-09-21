# 🐘 PostgreSQL Provider

`CoreSystem.Idempotency.PostgreSql` provides a PostgreSQL implementation of the `IIdempotencyStorage` abstraction.

It stores idempotency entries in a relational database using **Dapper**, making it an excellent choice for applications that already use PostgreSQL or require durable persistence across application restarts.

---

# Why PostgreSQL?

Choose the PostgreSQL provider when your application requires:

- Durable persistence
- Relational storage
- Transactional consistency
- Existing PostgreSQL infrastructure
- Long-lived idempotency records

If your primary goal is ultra-low latency, consider the **Redis provider** instead.

---

# Requirements

Before using the provider, ensure you have:

- PostgreSQL 16 or later
- An existing PostgreSQL database
- A valid connection string
- `CoreSystem.Idempotency`
- `CoreSystem.Idempotency.PostgreSql`

---

# Installation

Install the provider package.

```bash
dotnet add package CoreSystem.Idempotency.PostgreSql
```

Then register the provider.

```csharp
builder.Services
    .AddCoreIdempotency(options =>
    {
        builder.Configuration
            .GetSection("Core:Idempotency")
            .Bind(options);
    })
    .AddCoreIdempotencyPostgreSql(options =>
    {
        builder.Configuration
            .GetSection("ConnectionStrings")
            .Bind(options);
    });
```

---

# Database Setup

The provider automatically creates the required table and indexes during application startup.

> **Important**
>
> The PostgreSQL database must already exist.
> The provider creates tables and indexes, but it does **not** create databases.

Example:

```sql
CREATE DATABASE idempotency_db;
```

---

# Connection String

Configure the PostgreSQL connection string.

```json
{
  "ConnectionStrings": {
    "Idempotency": "Host=localhost;Port=5432;Database=idempotency_db;Username=admin;Password=admin"
  }
}
```

---

# Database Schema

The provider automatically creates the following table.

```sql
CREATE TABLE idempotency_keys
(
    key                 VARCHAR(255) PRIMARY KEY,
    request_fingerprint TEXT NULL,
    hash_algorithm      VARCHAR(255),
    status_code         INTEGER NOT NULL,
    content_type        VARCHAR(255),
    headers             BYTEA NOT NULL,
    body                BYTEA,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at          TIMESTAMPTZ NOT NULL
);

CREATE INDEX idx_idempotency_keys_expires_at
ON idempotency_keys (expires_at);
```

The schema is optimized for idempotency lookups and response replay.

---

# Expired Records

Expired entries are not removed automatically.

Schedule a periodic cleanup task.

Example:

```sql
DELETE
FROM idempotency_keys
WHERE expires_at <= NOW();
```

The cleanup frequency depends on your retention policy and expected request volume.

---

# Recommended Maintenance

For production environments, schedule regular database maintenance.

Recommended operations include:

- Cleanup of expired entries
- VACUUM
- ANALYZE

These operations help maintain query performance as the table grows.

---

# How It Works

The PostgreSQL provider participates in the idempotency workflow through the `IIdempotencyStorage` abstraction.

```text
Incoming Request
        │
        ▼
Generate Fingerprint
        │
        ▼
Lookup PostgreSQL
        │
  ┌─────┴─────┐
  │           │
Found      Not Found
  │           │
  ▼           ▼
Replay    Execute Endpoint
Response       │
               ▼
        Persist Response
```

The middleware coordinates the workflow while the PostgreSQL provider is responsible only for persistence.

---

# When to Use PostgreSQL

The PostgreSQL provider is recommended when:

- Your application already uses PostgreSQL.
- Strong durability is required.
- Responses must survive application restarts.
- Operational simplicity is preferred over distributed caching.

---

# Performance Considerations

Compared to in-memory or Redis-based storage, PostgreSQL typically provides:

- Higher durability
- Transactional guarantees
- Higher read/write latency
- Disk-based persistence

Choose the provider that best matches your application's consistency and performance requirements.

---

# Limitations

Keep the following considerations in mind:

- Expired entries require periodic cleanup.
- Persistence depends on database availability.
- Database maintenance is recommended for long-running deployments.

---

# Related Documentation

- Getting Started
- Configuration
- Architecture
- Request Lifecycle
- Redis Provider