# 🐘 PostgreSQL Provider

`CoreSystem.Idempotency.PostgreSql` provides a PostgreSQL implementation of the `IIdempotencyStorage` abstraction.

It stores idempotency entries in a relational database using **Dapper**, making it an excellent choice for applications that already use PostgreSQL or require durable persistence across application restarts.

---

## ❓ Why PostgreSQL?

Choose the PostgreSQL provider when your application requires:

- Durable persistence
- Relational storage
- Transactional consistency
- Existing PostgreSQL infrastructure
- Long-lived idempotency records

If your primary goal is ultra-low latency, consider the **Redis provider** instead.

---

## 📋 Requirements

Before using the provider, ensure you have:

- PostgreSQL 16 or later
- An existing PostgreSQL database
- A valid connection string
- `CoreSystem.Idempotency`
- `CoreSystem.Idempotency.PostgreSql`

---

## 📦 Installation

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
        options.ConnectionString =
            builder.Configuration.GetConnectionString("Idempotency");
    });
```

> **Important**
>
> `PostgreSqlOptions` exposes a single property, `ConnectionString`. Binding the whole
> `ConnectionStrings` section does not populate it, because that section's children are
> named `Redis` and `Idempotency`, not `ConnectionString`. Read the value explicitly, as
> shown above.
>
> If you leave `ConnectionString` null or empty, registration throws
> `InvalidOperationException` reporting that a PostgreSQL connection string is required.

---

## 🗄 Database Setup

The provider does **not** create any schema. You must apply the DDL yourself, once,
before the first request reaches the application.

> **Important**
>
> The provider never runs `CREATE TABLE` or `CREATE INDEX`. Applying the script is
> the caller's responsibility. The integration tests do this explicitly in their
> container fixture.
>
> The PostgreSQL database itself must also already exist; the provider does not
> create databases either.

1. Create the database:

```sql
CREATE DATABASE idempotency_db;
```

2. Apply the schema script shipped with the package at `scripts/postgres.sql`:

```bash
psql -h localhost -U admin -d idempotency_db -f scripts/postgres.sql
```

The script is idempotent, so it is safe to run on every deployment.

---

## 🔗 Connection String

Configure the PostgreSQL connection string.

```json
{
  "ConnectionStrings": {
    "Idempotency": "Host=localhost;Port=5432;Database=idempotency_db;Username=admin;Password=admin"
  }
}
```

---

## 🗃 Database Schema

The provider requires the following table. Apply it with `scripts/postgres.sql`,
which ships inside the NuGet package.

```sql
CREATE TABLE IF NOT EXISTS idempotency_keys
(
    key TEXT PRIMARY KEY,

    state TEXT NOT NULL DEFAULT 'completed' CHECK (state IN ('in_progress', 'completed')),

    lease_id TEXT,

    request_fingerprint TEXT,

    hash_algorithm TEXT,

    status_code INTEGER,

    content_type TEXT,

    headers BYTEA,

    body BYTEA,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    expires_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_idempotency_expires
    ON idempotency_keys (expires_at);
```

`state` and `lease_id` are required. The provider acquires ownership of a key by
inserting `state = 'in_progress'` together with a generated `lease_id`, and only the
request holding that lease may complete or release the entry. A table created without
those two columns fails on the first request.

The schema is optimized for idempotency lookups and response replay.

---

## ⏳ Expired Records

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

## 🔧 Recommended Maintenance

For production environments, schedule regular database maintenance.

Recommended operations include:

- Cleanup of expired entries
- VACUUM
- ANALYZE

These operations help maintain query performance as the table grows.

---

## 🔄 How It Works

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

## 🧩 When to Use PostgreSQL

The PostgreSQL provider is recommended when:

- Your application already uses PostgreSQL.
- Strong durability is required.
- Responses must survive application restarts.
- Operational simplicity is preferred over distributed caching.

---

## ⏱️ Performance Considerations

Compared to in-memory or Redis-based storage, PostgreSQL typically provides:

- Higher durability
- Transactional guarantees
- Higher read/write latency
- Disk-based persistence

Choose the provider that best matches your application's consistency and performance requirements.

---

## ⚠️ Limitations

Keep the following considerations in mind:

- Expired entries require periodic cleanup.
- Persistence depends on database availability.
- Database maintenance is recommended for long-running deployments.

---

## 📚 Related Documentation

- Getting Started
- Configuration
- Architecture
- Request Lifecycle
- Redis Provider