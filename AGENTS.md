# AGENTS.md

## 1. Engineering Standards

Before performing any task, read `CORE-SYSTEM-STANDARDS.md`.

These standards are mandatory and apply to all development
activities in this repository.

The repository-specific instructions in this file complement
the common standards.

If a project-specific rule differs from a common standard,
follow the explicit project-specific exception.

## 2. Repository Overview

CoreSystem.Idempotency is an idempotency framework
for ASP.NET Core.

- Solution: CoreSystem.Idempotency.sln
- SDK: 10.0.201
- Target frameworks: net8.0;net9.0;net10.0
- Nullable: enabled
- ImplicitUsings: enabled

Follow CORE-SYSTEM-STANDARDS.md for common
engineering rules.

## 3. Packages

| Project | Package ID |
|---|---|
| Core.Idempotency | CoreSystem.Idempotency |
| Core.Idempotency.Redis | CoreSystem.Idempotency.Redis |
| Core.Idempotency.PostgreSql | CoreSystem.Idempotency.PostgreSql |

## 4. Architecture

- Core must remain storage-agnostic.
- Providers depend on Core.
- Core must never reference Redis or PostgreSQL providers.
- Storage extensions must implement IIdempotencyStorage.
- New providers must be independent packages.

Extensibility contracts:

- IIdempotencyStorage
- IIdempotencyKeyResolver
- IRequestFingerprintProvider
- IRequestFingerprintBuilder
- IRequestHasher

Storage lifecycle is lease-based.
Changes to its semantics are breaking changes
for all providers.

## 5. Dependencies

- Use PackageReference for published core dependencies.
- Never introduce provider dependencies into Core.
- Providers must reference published core versions.
- Publish Core before its providers.
- Manage versions centrally.
- Regenerate lock files after dependency changes.

## 6. Code Conventions

- File-scoped namespaces.
- Four-space indentation.
- Use var unless the type is ambiguous.
- Primary constructors for implementation classes.
- Minimal visibility.
- XML documentation for all public API members.
- Centralize exception and message strings.
- Use ArgumentNullException.ThrowIfNull.
- Configuration belongs in Options classes.
- Observability must use IdempotencyMetrics
  and the existing diagnostics infrastructure.

## 7. Testing

- Unit tests: xUnit v3, FluentAssertions and Moq.
- Integration tests: Testcontainers.
- Real Redis and PostgreSQL behavior belongs
  in integration tests.
- Shared storage behavior must be tested
  through IdempotencyTestsBase.
- Test both providers when shared storage contracts change.

## 8. PostgreSQL Schema

Keep these files synchronized:

- src/Core.Idempotency.PostgreSql/scripts/postgres.sql
- tests/Core.Idempotency.PostgreSql.IntegrationTests/
  Scripts/postgresql.sql

Every schema change requires integration test coverage.

## 9. Build and Commands

dotnet build CoreSystem.Idempotency.sln -c Release
dotnet test --solution CoreSystem.Idempotency.sln -c Release

## 10. CI/CD and Release

- GitHub Actions.
- Tag format: <project folder>/v<version>.
- Publish Core before providers.
- Published versions are immutable.
- Keep assembly and package versions synchronized.
- Update CHANGELOG, README and documentation.
- Preserve publish.yml test-project mappings.
- Verify package contents before publishing.

## 11. Documentation

- MkDocs Material.
- Maintain mkdocs.yml navigation.
- Synchronize configuration documentation
  with Options classes and DI extensions.
- Update README files for affected packages.

## 13. Memory

Read MEMORY.md at the beginning of a task.
Update it when significant decisions or project state change.
Keep it concise (max ~50 lines: summarize or drop what no longer
adds value).
If something becomes a permanent rule, propose moving it to
`AGENTS.md` instead of keeping it in memory.
Never store sensitive data (keys, tokens, personal data).
