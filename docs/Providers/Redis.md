# 🔴 Redis Provider

`CoreSystem.Idempotency.Redis` provides a Redis implementation of the `IIdempotencyStorage` abstraction.

It stores idempotency entries in a distributed cache, making it the recommended provider for most production workloads that require low latency, horizontal scalability, and distributed request processing.

---

# Why Redis?

Choose the Redis provider when your application requires:

- Low-latency request processing
- Distributed idempotency across multiple instances
- Horizontal scalability
- Cloud-native deployments
- High request throughput

If long-term durability is your primary requirement, consider the **PostgreSQL provider** instead.

---

# Requirements

Before using the provider, ensure you have:

- Redis 7 or later
- A reachable Redis instance
- `CoreSystem.Idempotency`
- `CoreSystem.Idempotency.Redis`

---

# Installation

Install the provider package.

```bash
dotnet add package CoreSystem.Idempotency.Redis
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

Then register the Redis provider.

```csharp
builder.Services
    .AddCoreIdempotencyRedis(options =>
    {
        builder.Configuration
            .GetSection("ConnectionStrings")
            .Bind(options);
    });
```

---

# Connection String

Configure the Redis connection string.

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

---

# How It Works

The Redis provider participates in the idempotency workflow through the `IIdempotencyStorage` abstraction.

```text
Incoming Request
        │
        ▼
Generate Fingerprint
        │
        ▼
Lookup Redis
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

The middleware coordinates the workflow while the Redis provider is responsible only for persisting and retrieving idempotency entries.

---

# Expiration

Stored responses automatically expire after the configured expiration period.

```csharp
options.Expiration =
    TimeSpan.FromHours(24);
```

Once an entry expires:

- It is automatically removed by Redis.
- The next request is treated as a new operation.
- A new response is generated and persisted.

---

# Production Recommendations

For production environments, consider the following recommendations.

- Deploy Redis in a highly available configuration.
- Configure persistence according to your recovery requirements.
- Monitor memory usage and eviction policies.
- Use dedicated Redis instances whenever possible.
- Configure key expiration consistently with your business requirements.

---

# Performance Characteristics

Redis is optimized for high-throughput, low-latency workloads.

Typical characteristics include:

- Extremely fast reads and writes
- Distributed access across multiple application instances
- Automatic key expiration
- In-memory storage
- Minimal serialization overhead

This makes Redis an excellent default choice for most distributed APIs.

---

# Limitations

Keep the following considerations in mind.

- Data durability depends on the configured Redis persistence policy.
- Memory capacity determines the maximum number of stored entries.
- Eviction policies may remove entries before they naturally expire if memory pressure occurs.

Applications requiring durable persistence should consider the PostgreSQL provider.

---

# Choosing Between Redis and PostgreSQL

| Requirement | Redis | PostgreSQL |
|-------------|:-----:|:----------:|
| Low latency | ✅ | ⚪ |
| Horizontal scalability | ✅ | ⚪ |
| Automatic expiration | ✅ | ⚪ |
| Durable persistence | ⚪ | ✅ |
| Relational storage | ⚪ | ✅ |
| High request throughput | ✅ | ⚪ |

---

# Related Documentation

- Getting Started
- Configuration
- Architecture
- Request Lifecycle
- PostgreSQL Provider