# ⚡ OpenAPI / Swagger

> **Documents the `Idempotency-Key` header on idempotent operations.**

CoreSystem.Idempotency.OpenApi integrates with Swashbuckle to surface the `Idempotency-Key` header in the generated OpenAPI document.

## 📦 Installation

```bash
dotnet add package CoreSystem.Idempotency.OpenApi
```

## 🚀 Quick Start

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<IdempotencyHeaderOperationFilter>();
});
```

## 🧩 Component

- `IdempotencyHeaderOperationFilter` — `IOperationFilter` that adds the header when the HTTP method is in `AllowedMethods`.