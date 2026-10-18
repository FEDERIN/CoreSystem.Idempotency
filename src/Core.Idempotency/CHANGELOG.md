# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/).

---

## [2.0.2] - 2026-10-18

Dependency refresh. No code or API changes.

### Changed

- `CoreSystem.Observability.Abstractions` 1.0.1 to 1.0.2.
- `CoreSystem.Serialization` 1.2.3 to 1.2.4, which pulls MessagePack 3.1.8 to 3.1.12 and
  protobuf-net 3.2.56 to 3.4.30.
- `OpenTelemetry.Extensions.Hosting` 1.19.0 to 1.20.0.
- Refreshed transitive graph: `Microsoft.Extensions.Diagnostics.HealthChecks` 8.0.28 to
  8.0.32 and the `System.Diagnostics.DiagnosticSource` 10.0.0 chain.

The point of this release is that the dependency floors declared in the nuspec now match
the versions the assembly was actually built and tested against. 2.0.1 declared
`CoreSystem.Observability.Abstractions >= 1.0.1` and `CoreSystem.Serialization >= 1.2.3`
while the pinned versions had moved on.

---

## [2.0.1] - 2026-10-13

Corrections release. Version 2.0.0 shipped documentation that did not compile and a
PostgreSQL schema that omitted two mandatory columns. No runtime behaviour of the
middleware changed.

### Fixed

- Corrected the `AddCoreIdempotencyRedis` and `AddCoreIdempotencyPostgreSql` examples in the
  package READMEs. Both methods require an `Action<TOptions> configure` argument, so the
  documented parameterless calls did not compile.
- Replaced the documented provider-registration example in the docs site, which bound
  `ConnectionStrings` to an options type that cannot receive it. It compiled but threw
  `InvalidOperationException` at startup.
- Replaced the placeholder provider-registration examples in Getting Started and
  Configuration, which showed `// Configure Redis` and left the options unset, so they threw
  at startup when copied verbatim.
- Corrected the PostgreSQL provider description and documentation, which claimed the provider
  creates its schema automatically. It never has: the provider issues no `CREATE TABLE` or
  `CREATE INDEX`.
- Replaced the documented `idempotency_keys` schema, which predated the lease-based storage
  contract and omitted the mandatory `state` and `lease_id` columns. Creating the table from
  the published DDL caused the first request to fail.
- Corrected `PackageProjectUrl`, which returned 404, and `RepositoryUrl`, which named an
  unrelated repository.
- Corrected the problem-details `type` URI on `IdempotencyFingerprintMismatchException`, which
  returned 404.
- Corrected the `site_url` in `mkdocs.yml`, which used a lowercase path segment that returned
  404.
- Normalized the `request_fingerprint` and `hash_algorithm` column casing in the PostgreSQL
  `SetAsync` statement to match the rest of the provider and the shipped schema.

### Added

- XML documentation is now generated and published for all three packages, so the public API
  carries IntelliSense.
- Debug symbols are embedded in the assemblies and SourceLink is enabled, so stepping through
  the package from nuget.org resolves to the original source.
- The correct `idempotency_fingerprint_mismatch` error page documents
  the current problem-details contract.
- The PostgreSQL package now ships `scripts/postgres.sql` in the `.nupkg`, so consumers can
  create the table without reconstructing the DDL from the documentation.
- XML documentation comments on all public members of the storage contract, key resolver,
  models, metrics and registration extensions.

### Removed

- Removed the three `CoreSystem.Idempotency*` entries from `Directory.Packages.props`. They
  were inert, and a future `PackageReference` would have silently resolved to the published
  2.0.0 instead of the local project.

### Migration

No code changes are required. Upgrade to 2.0.1 to obtain correct documentation.

If you created the `idempotency_keys` table from the 2.0.0 documentation, it is missing
`state` and `lease_id`. Drop it and re-apply `scripts/postgres.sql`, which is idempotent.

### Release process

Internal to this repository, listed because 2.0.0 was published outside the tag workflow and
the workflow had therefore never been exercised.

- `publish.yml` mapped only projects from an unrelated repository, so no package here could be
  published from a tag. It now maps the three packages in this repository and also runs their
  integration tests before publishing.
- `publish.yml` overrode only `Version` on the command line, so a release shipped an assembly
  whose `AssemblyVersion` and `FileVersion` still carried the previous version. All four version
  properties are now passed, and a verification step fails the job if they disagree with the tag.
- Version 2.0.0 records a git commit that does not exist in this repository, so the published
  package could not be reproduced from source.

---

## [2.0.0] - 2026-08-07

### Added

- Introduced a provider-based storage architecture.
- Added the `CoreSystem.Idempotency.Redis` package.
- Added the `CoreSystem.Idempotency.PostgreSql` package.
- Added the `IIdempotencyStorage` abstraction for pluggable storage providers.

### Changed

- Refactored `CoreSystem.Idempotency` to be completely storage-provider independent.
- Storage implementations are now distributed as independent NuGet packages.
- Simplified the core package by removing provider-specific dependencies.
- Updated documentation to reflect the new modular architecture.

### Removed

- Removed built-in Redis storage implementation from `CoreSystem.Idempotency`.
- Removed built-in PostgreSQL storage implementation from `CoreSystem.Idempotency`.

### Migration

Applications upgrading from **1.x** must:

1. Install a storage provider package:
   - `CoreSystem.Idempotency.Redis`, or
   - `CoreSystem.Idempotency.PostgreSql`.
2. Register the selected provider during application startup.
3. Remove any legacy provider configuration from `CoreSystem.Idempotency`.
