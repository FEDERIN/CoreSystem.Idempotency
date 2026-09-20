# ❓ Why CoreSystem.Idempotency?

HTTP retries are an inevitable part of modern distributed systems.

Clients retry requests because of network failures, timeouts, gateway errors, or uncertain response states. While retries improve reliability, they also introduce an important challenge: ensuring that the same business operation is never executed more than once.

Operations such as payment processing, order creation, inventory updates, and financial transactions require protection against duplicate execution when clients retry a request.

Implementing idempotency independently in every application often results in duplicated middleware, inconsistent persistence strategies, limited observability, and tightly coupled infrastructure.

CoreSystem.Idempotency provides a unified, provider-based idempotency platform that solves these concerns while allowing applications to remain focused on business logic.

---

# The Problem

Modern distributed applications commonly require capabilities such as:

- Preventing duplicate execution of critical operations.
- Persisting idempotency state across multiple application instances.
- Returning the original response for repeated requests.
- Detecting when an idempotency key is reused with different request data.
- Supporting multiple storage technologies.
- Measuring request execution and storage performance.
- Integrating with OpenTelemetry.
- Remaining extensible without modifying application code.

Although these requirements are common, they are often implemented differently in every application, leading to duplicated infrastructure and inconsistent behavior.

---

# The Solution

CoreSystem.Idempotency acts as the orchestration layer of the CoreSystem idempotency ecosystem.

Instead of embedding storage-specific logic into the middleware, the framework coordinates request processing through a provider-independent architecture.

The middleware is responsible for:

- Validating idempotency requests.
- Generating request fingerprints.
- Coordinating response replay.
- Delegating persistence through `IIdempotencyStorage`.
- Publishing OpenTelemetry metrics.

Storage implementations are distributed as independent provider packages.

This architecture allows applications to switch persistence technologies—or introduce entirely new providers—without modifying the middleware.

---

# Package Architecture

| Package | Responsibility |
|----------|----------------|
| **CoreSystem.Idempotency** | Middleware, request fingerprinting, response replay, storage abstractions, and orchestration |
| **CoreSystem.Idempotency.Redis** | Redis storage provider |
| **CoreSystem.Idempotency.PostgreSql** | PostgreSQL storage provider |
| **CoreSystem.Serialization** | Serialization infrastructure |
| **CoreSystem.Http** | HTTP abstractions |
| **CoreSystem.Redis** | Redis connectivity infrastructure |
| **CoreSystem.Observability** *(Optional)* | OpenTelemetry integration |
| **CoreSystem.Observability.Abstractions** | Shared observability contracts |

Applications depend only on the CoreSystem.Idempotency API while selecting the storage provider that best fits their environment.

---

# Benefits

Using CoreSystem.Idempotency provides several advantages.

## Reliability

- Concurrent duplicate suppression for critical operations.
- Automatic response replay.
- Request fingerprint validation.
- Protection against accidental key reuse.

---

## Extensibility

- Provider-independent architecture.
- Pluggable storage providers.
- Consistent storage contract through `IIdempotencyStorage`.
- Future providers can be added without modifying the core framework.

---

## Operational Excellence

- Built-in OpenTelemetry metrics.
- Consistent serialization across providers.
- Production-ready defaults.
- Separation of business logic from infrastructure.

---

# When Should You Use CoreSystem.Idempotency?

CoreSystem.Idempotency is recommended for applications that expose state-changing operations, including:

- Payment processing
- Order management
- Inventory management
- Financial transactions
- Distributed APIs
- Microservices
- Cloud-native applications
- Public APIs exposed to unreliable networks

If duplicate execution could produce inconsistent business results, idempotency should be considered part of the application's architecture.

---

# Why a Provider-Based Architecture?

Separating the middleware from persistence provides several advantages.

- The core framework remains storage-independent.
- Storage providers evolve independently.
- Applications choose only the provider they need.
- New providers can be introduced without changing the public API.
- Monitoring, documentation, and behavior remain consistent across providers.

This architecture keeps CoreSystem.Idempotency focused on coordinating idempotent request processing while allowing persistence technologies to evolve independently.

---

# Next Steps

Continue with the following guides to learn more:

- **Getting Started** — Install and configure the framework.
- **Architecture** — Understand the middleware and provider model.
- **Configuration** — Configure framework behavior.
- **Storage Providers** — Choose and configure a persistence provider.
- **Fingerprinting** — Learn how request identity is validated.
