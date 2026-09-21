# 🚀 Getting Started

Welcome to **CoreSystem.Idempotency**, a production-ready idempotency framework for **.NET 8**.

In this guide you'll learn how to:

- Install the framework
- Install a storage provider
- Configure the middleware
- Register the services
- Execute your first idempotent request

> **Estimated time:** 5 minutes

---

# Prerequisites

Before getting started, ensure you have:

- .NET 8 SDK
- An ASP.NET Core application
- One supported storage provider

---

# Choose a Storage Provider

`CoreSystem.Idempotency` requires a storage provider to persist idempotency entries.

Currently supported providers:

- **CoreSystem.Idempotency.Redis**
- **CoreSystem.Idempotency.PostgreSql**

Each provider has its own installation, configuration, and operational guidance.

See **Storage Providers** for more information.

---

# Step 1 — Install the Framework

Install the core framework.

```bash
dotnet add package CoreSystem.Idempotency
```

The package contains:

- Idempotency middleware
- Request fingerprinting
- Response replay
- Storage abstractions
- Built-in observability

---

# Step 2 — Install a Storage Provider

Choose the provider that best fits your application.

### Redis

```bash
dotnet add package CoreSystem.Idempotency.Redis
```

### PostgreSQL

```bash
dotnet add package CoreSystem.Idempotency.PostgreSql
```

---

# Step 3 — Configure the Framework

Configure the framework in `appsettings.json`.

```json
{
  "Core": {
    "Idempotency": {
      "Enabled": true,
      "InstanceName": "Orders"
    }
  }
}
```

!!! note

    The configuration shown above contains only the minimum required settings.
    See **Configuration** for the complete list of available options.

---

# Step 4 — Register the Services

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

Then register the storage provider.

### Redis

```csharp
builder.Services.AddCoreIdempotencyRedis(options =>
{
    // Configure Redis
});
```

### PostgreSQL

```csharp
builder.Services.AddCoreIdempotencyPostgreSql(options =>
{
    // Configure PostgreSQL
});
```

`AddCoreIdempotency()` registers the middleware, request fingerprinting services, diagnostics, and storage abstractions.

The provider package registers the corresponding `IIdempotencyStorage` implementation.

---

# Step 5 — Enable the Middleware

Configure the ASP.NET Core request pipeline.

```csharp
var app = builder.Build();

app.UseCoreIdempotency();

app.Run();
```

---

# Step 6 — Send an Idempotent Request

Include an idempotency key in every request that needs duplicate suppression and response replay.

```http
POST /orders HTTP/1.1
Idempotency-Key: 8db99b84-6b57-41e3-ae66-98c4d4a2d9d5
Content-Type: application/json

{
  "productId": 1,
  "quantity": 2
}
```

The first request is processed normally and its response is persisted by the configured storage provider.

Subsequent requests using the same idempotency key and an identical request fingerprint receive the previously stored response without executing the endpoint again.

---

# Next Steps

Now that the framework is running, continue with:

- **Configuration**
- **Architecture**
- **Fingerprinting**
- **Storage Providers**
