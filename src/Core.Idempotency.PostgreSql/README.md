# 🐘 CoreSystem.Idempotency.PostgreSql

> **PostgreSQL storage provider for CoreSystem.Idempotency, built on the `IIdempotencyStorage` abstraction.**

It is recommended for applications that require transactional consistency and long-lived storage.

![NuGet](https://img.shields.io/nuget/v/CoreSystem.Idempotency.PostgreSql?style=for-the-badge)
![Downloads](https://img.shields.io/nuget/dt/CoreSystem.Idempotency.PostgreSql?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8.0-blue?style=for-the-badge)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16+-336791?style=for-the-badge&logo=postgresql&logoColor=white)
![Dapper](https://img.shields.io/badge/Dapper-ORM-orange?style=for-the-badge)

It provides a durable implementation of `IIdempotencyStorage`, enabling reliable persistence of idempotency entries using PostgreSQL and Dapper.

---

## ✨ Features

- ✅ Durable persistence
- ✅ PostgreSQL storage
- ✅ Dapper-based implementation
- ✅ Idempotent schema script shipped with the package
- ✅ Seamless integration with CoreSystem.Idempotency
- ✅ OpenTelemetry compatible
- ✅ Production-ready

---

## 📦 Installation

Install the framework.

```bash
dotnet add package CoreSystem.Idempotency
```

Install the PostgreSQL provider.

```bash
dotnet add package CoreSystem.Idempotency.PostgreSql
```

---

## 🚀 Quick Start

Configure PostgreSQL and register the provider.

```json
{
  "ConnectionStrings": {
    "Idempotency": "Host=localhost;Port=5432;Database=idempotency_db;Username=admin;Password=admin"
  }
}
```

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

`PostgreSqlOptions.ConnectionString` must be assigned explicitly; the provider throws at
startup if it is null or empty.

Enable the middleware.

```csharp
app.UseCoreIdempotency();
```

---

## 🗄 Database Setup

The provider does **not** create any schema. Apply the DDL yourself before the first request.

The database must already exist.

```sql
CREATE DATABASE idempotency_db;
```

Then apply the schema script shipped inside the package at `scripts/postgres.sql`.

```bash
psql -h localhost -U admin -d idempotency_db -f scripts/postgres.sql
```

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

> **Important**
>
> `state` and `lease_id` are mandatory. The provider claims ownership of a key by writing
> `state = 'in_progress'` with a generated `lease_id`; only the holder of that lease may
> complete or release the entry. A table created without those columns fails on the
> first request.

The script is idempotent and safe to run on every deployment.

---

## ⚡ Why PostgreSQL?

PostgreSQL is ideal for:

- Financial systems
- Transactional applications
- Long-term persistence
- Existing PostgreSQL infrastructure
- Applications requiring durability

---

## 📊 Characteristics

| Feature | Supported |
|----------|:---------:|
| Durable Persistence | ✅ |
| Relational Storage | ✅ |
| Schema Script Included | ✅ (`scripts/postgres.sql`) |
| Distributed | ✅ |
| Automatic Cleanup | ⚪ (Requires scheduled maintenance) |

---

## 📚 Documentation

Complete documentation includes:

- Installation
- Database schema
- Configuration
- Maintenance
- Performance considerations

---

## 🤝 Contributing

Contributions, bug reports and feature requests are always welcome. Fork the repository, create a feature branch, and open a pull request.

---

## 📄 License

Released under the MIT License.