# 🐘 CoreSystem.Idempotency.PostgreSql

![NuGet](https://img.shields.io/nuget/v/CoreSystem.Idempotency.PostgreSql?style=for-the-badge)
![Downloads](https://img.shields.io/nuget/dt/CoreSystem.Idempotency.PostgreSql?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8.0-blue?style=for-the-badge)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16+-336791?style=for-the-badge&logo=postgresql&logoColor=white)
![Dapper](https://img.shields.io/badge/Dapper-ORM-orange?style=for-the-badge)

PostgreSQL storage provider for **CoreSystem.Idempotency**.

CoreSystem.Idempotency.PostgreSql provides a durable implementation of `IIdempotencyStorage`, enabling reliable persistence of idempotency entries using PostgreSQL.

It is recommended for applications that require transactional consistency and long-lived storage.

---

# ✨ Features

- ✅ Durable persistence
- ✅ PostgreSQL storage
- ✅ Dapper-based implementation
- ✅ Automatic schema creation
- ✅ Seamless integration with CoreSystem.Idempotency
- ✅ OpenTelemetry compatible
- ✅ Production-ready

---

# 📦 Installation

Install the framework.

```bash
dotnet add package CoreSystem.Idempotency
```

Install the PostgreSQL provider.

```bash
dotnet add package CoreSystem.Idempotency.PostgreSql
```

---

# 🚀 Quick Start

Configure PostgreSQL.

```json
{
  "ConnectionStrings": {
    "Idempotency": "Host=localhost;Port=5432;Database=idempotency_db;Username=admin;Password=admin"
  }
}
```

Register the framework.

```csharp
builder.Services
    .AddCoreIdempotency(options =>
    {
        builder.Configuration
            .GetSection("Core:Idempotency")
            .Bind(options);
    });
```

Register the PostgreSQL provider.

```csharp
builder.Services.AddCoreIdempotencyPostgreSql();
```

Enable the middleware.

```csharp
app.UseCoreIdempotency();
```

---

# ⚡ Why PostgreSQL?

PostgreSQL is ideal for:

- Financial systems
- Transactional applications
- Long-term persistence
- Existing PostgreSQL infrastructure
- Applications requiring durability

---

# 📊 Characteristics

| Feature | Supported |
|----------|:---------:|
| Durable Persistence | ✅ |
| Relational Storage | ✅ |
| Automatic Schema Creation | ✅ |
| Distributed | ✅ |
| Automatic Cleanup | ⚪ (Requires scheduled maintenance) |

---

# 📚 Documentation

Complete documentation includes:

- Installation
- Database schema
- Configuration
- Maintenance
- Performance considerations

---

# 📄 License

MIT License © Federin Pastor Gutierrez Ortiz