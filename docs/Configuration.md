# ⚙️ Configuration

This guide describes every configuration option available in **CoreSystem.Idempotency**.

You'll learn how to configure:

- Framework behavior
- Request fingerprinting
- Supported HTTP methods
- Response expiration
- Instance naming
- `appsettings.json` integration

> **Note**
>
> Storage providers are configured independently.
> See the provider-specific documentation for Redis and PostgreSQL configuration.

---

# Configuration Overview

Configure the framework using the `AddCoreIdempotency()` extension.

```csharp
builder.Services.AddCoreIdempotency(options =>
{
    // Configure the framework here
});
```

The framework can also be configured using `appsettings.json`.

---

# Configuration Options

| Option | Description | Default |
|----------|-------------|---------|
| `Enabled` | Enables or disables the middleware | `true` |
| `InstanceName` | Optional prefix used by storage providers | `null` |
| `Expiration` | Lifetime of persisted responses | `30 minutes` |
| `AllowedMethods` | HTTP methods protected by the middleware | `POST`, `PUT` |
| `Fingerprint` | Request fingerprint generation options | Default configuration |

---

# Enable or Disable the Framework

Enable the middleware.

```csharp
options.Enabled = true;
```

Disable the middleware without removing it from the dependency injection container.

```csharp
options.Enabled = false;
```

---

# Instance Name

`InstanceName` allows multiple applications or environments to safely share the same storage infrastructure.

Storage providers may use this value to prefix persisted keys and avoid collisions.

```csharp
options.InstanceName = "Orders";
```

Typical examples include:

- Production
- Staging
- Development
- Multi-tenant applications

---

# Allowed HTTP Methods

By default, the middleware protects:

- POST
- PUT

Add additional methods:

```csharp
options.AddAllowedMethods(
    "PATCH",
    "DELETE");
```

Remove methods:

```csharp
options.RemoveAllowedMethods(
    "PUT");
```

Requests using methods that are not configured bypass the middleware.

---

# Response Expiration

Configure how long an idempotent response remains available for replay.

```csharp
options.Expiration =
    TimeSpan.FromHours(24);
```

After the expiration period, the request is treated as a new operation.

---

# Request Fingerprinting

Fingerprinting prevents an idempotency key from being reused with a different request.

Example:

```csharp
options.Fingerprint.IncludedHeaders.Add("X-Tenant-Id");

options.Fingerprint.IncludedHeaders.Add("X-Region");
```

Additional options are also available.

```csharp
options.Fingerprint.IncludeQueryString = true;

options.Fingerprint.IncludeContentType = true;
```

See **Fingerprinting** for a complete description of every available option.

---

# Using appsettings.json

```json
{
  "Core": {
    "Idempotency": {
      "Enabled": true,
      "InstanceName": "Orders",
      "Expiration": "1.00:00:00",
      "AllowedMethods": [
        "POST",
        "PUT"
      ],
      "Fingerprint": {
        "Enabled": true,
        "IncludeQueryString": true,
        "IncludeContentType": true,
        "IncludedHeaders": [
          "X-Tenant-Id",
          "X-Region"
        ]
      }
    }
  }
}
```

Bind the configuration.

```csharp
builder.Services.AddCoreIdempotency(options =>
{
    builder.Configuration
        .GetSection("Core:Idempotency")
        .Bind(options);
});
```

!!! note

    Storage provider configuration is performed separately.
    See the Redis or PostgreSQL provider documentation for provider-specific settings.

---

# Storage Provider Configuration

`CoreSystem.Idempotency` does not configure storage providers.

After registering the framework, configure the provider package independently.

Example:

```csharp
builder.Services
    .AddCoreIdempotency(options =>
    {
        options.InstanceName = "Orders";
    })
    .AddCoreIdempotencyRedis(options =>
    {
        // Redis configuration
    });
```

or

```csharp
builder.Services
    .AddCoreIdempotency(options =>
    {
        options.InstanceName = "Orders";
    })
    .AddCoreIdempotencyPostgreSql(options =>
    {
        // PostgreSQL configuration
    });
```

---

# Recommended Configurations

## Body limits

The middleware bypasses idempotency for requests larger than `MaxRequestBodySizeBytes` or with an excluded content type prefix. `multipart/` is excluded by default. Responses larger than `MaxResponseBodySizeBytes` are returned normally but are not persisted or replayable.

```csharp
builder.Services.AddCoreIdempotency(options =>
{
    options.MaxRequestBodySizeBytes = 64 * 1024;
    options.MaxResponseBodySizeBytes = 256 * 1024;
    options.ExcludedContentTypePrefixes.Add("application/grpc");
});
```

The response-capture implementation comes from `CoreSystem.Http`; applications that stream large responses should exclude those endpoints from this middleware until the capture implementation itself supports bounded streaming.

---

## Key scope and validation

The default resolver accepts exactly one `Idempotency-Key`, rejects surrounding whitespace, and limits it to 128 characters. It derives an opaque storage key from the application instance, host, HTTP method, route, and client key.

For multi-tenant APIs, configure a stable tenant or user scope. Do not use a display name or any value that can change between retries.

```csharp
builder.Services.AddCoreIdempotency(options =>
{
    options.InstanceName = "Orders";
    options.ScopeResolver = context => context.User.FindFirst("tenant_id")?.Value;
});
```

Applications with a different scope model can replace `IIdempotencyKeyResolver`; custom resolvers must provide equivalent validation and isolation.

---

## Development

| Setting | Value |
|----------|-------|
| Instance Name | Development |
| Expiration | 15 minutes |
| Fingerprint | Default |

---

## Production

| Setting | Value |
|----------|-------|
| Instance Name | Application name |
| Expiration | 24 hours |
| Fingerprint | Default + business headers |

---

# Best Practices

- Configure an `InstanceName` when multiple applications share the same storage.
- Use UUIDs for idempotency keys.
- Configure expiration according to your business requirements.
- Protect only operations that modify application state.
- Never reuse an idempotency key for different requests.
- Include only stable headers when customizing request fingerprinting.
