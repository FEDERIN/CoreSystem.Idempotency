# ♻️ Response Replay

Response replay is one of the core features of **CoreSystem.Idempotency**.

After a successful request execution, the middleware persists the generated HTTP response together with its request fingerprint.

When an identical request is received with the same idempotency key, the stored response is replayed immediately instead of executing the application endpoint again.

This replays persisted responses while returning deterministic responses across retries. It also prevents concurrent requests with the same key from executing simultaneously.

---

# How It Works

When an incoming request reaches the middleware, the following sequence occurs.

```text
                  Incoming Request
                         │
                         ▼
          Resolve Idempotency Key
                         │
                         ▼
          Generate Request Fingerprint
                         │
                         ▼
          Lookup IIdempotencyStorage
                         │
         ┌───────────────┴───────────────┐
         │                               │
         ▼                               ▼
     Entry Found                   Entry Not Found
         │                               │
         ▼                               ▼
 Compare Fingerprint            Execute Endpoint
         │                               │
         ▼                               ▼
 Fingerprint Match?             Capture Response
         │                               │
   ┌─────┴─────┐                         ▼
   │           │                 Persist Response
   │           │                         │
   ▼           ▼                         ▼
Replay      Throw                 Return Response
Response    Exception
```

---

# What Is Stored?

The middleware persists all the information required to faithfully reproduce the original response.

| Property | Description |
|----------|-------------|
| Status Code | HTTP response status code. |
| Headers | HTTP response headers. |
| Body | Serialized response body. |
| Content Type | Original response content type. |
| Request Fingerprint | Validates that future requests represent the same logical operation. |
| Expiration | Determines when the stored entry becomes invalid. |

The storage format is implementation-specific, but every provider persists the same logical information.

---

# First Request

The first request executes normally.

```http
POST /orders
Idempotency-Key: order-001
```

```text
Application
      │
      ▼
Business Logic Executes
      │
      ▼
Response Captured
      │
      ▼
Persist Response
      │
      ▼
Return Response
```

---

# Duplicate Request

If the same request is received again using the same idempotency key and the same request fingerprint:

```http
POST /orders
Idempotency-Key: order-001
```

The application endpoint is **not executed**.

Instead:

```text
Lookup Stored Entry
        │
        ▼
Replay Stored Response
        │
        ▼
Return Response
```

This avoids duplicate business operations while significantly reducing processing time.

---

# Fingerprint Validation

Before replaying a stored response, the middleware compares the incoming request fingerprint with the persisted fingerprint.

If both fingerprints match, the cached response is replayed.

If they differ, the middleware throws an `IdempotencyFingerprintMismatchException`.

This prevents an idempotency key from being reused for a different logical operation.

See **Fingerprinting** for implementation details.

---

# Response Expiration

Stored responses remain available until the configured expiration period elapses.

```csharp
options.Expiration =
    TimeSpan.FromHours(24);
```

Once the entry expires:

- The stored response is no longer replayed.
- The next request is treated as a new operation.
- A new response is generated and persisted.

---

# Cacheable Status Codes

By default, the middleware persists successful responses.

Default status codes include:

- 200 OK
- 201 Created
- 202 Accepted
- 204 No Content

Additional status codes can be configured through `IdempotencyOptions`.

```csharp
options.AddCacheableStatusCodes(206);

options.RemoveCacheableStatusCodes(202);
```

---

# Storage Independence

Response replay is completely independent of the persistence technology.

Whether the application uses:

- CoreSystem.Idempotency.Redis
- CoreSystem.Idempotency.PostgreSql
- A custom provider

the replay behavior remains exactly the same because every provider implements the `IIdempotencyStorage` contract.

---

# Best Practices

- Enable response replay only for operations that modify application state.
- Configure an expiration period appropriate for your business process.
- Generate a unique idempotency key for every logical operation.
- Keep request fingerprint validation enabled.
- Avoid replaying responses for non-idempotent business scenarios.

---

# Related Documentation

- Architecture
- Configuration
- Fingerprinting
- Request Lifecycle
- Storage Providers
