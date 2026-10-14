# 🧭 Roadmap

This page tracks work that is known to be outstanding. Items are grouped by intent, not by
date: no release is scheduled until each one is agreed and implemented.

---

## 📦 Current Release

**2.0.1** is a corrections release. Version 2.0.0 shipped documentation that did not compile
and a PostgreSQL schema missing two mandatory columns, so 2.0.1 fixes the documentation,
publishes XML documentation and debug symbols, and corrects the package metadata URLs.

| Package | Version |
|---------|:-------:|
| [**CoreSystem.Idempotency**](https://www.nuget.org/packages/CoreSystem.Idempotency) | 2.0.1 |
| [**CoreSystem.Idempotency.Redis**](https://www.nuget.org/packages/CoreSystem.Idempotency.Redis) | 2.0.1 |
| [**CoreSystem.Idempotency.PostgreSql**](https://www.nuget.org/packages/CoreSystem.Idempotency.PostgreSql) | 2.0.1 |

See the changelog for the full list.

---

## 🧹 Housekeeping

- **Dead central package pins.** `Directory.Packages.props` still declares about 27 packages
  that no project references and that never resolve, including `xunit` 2.5.3 beside the
  `xunit.v3` the repository actually uses, and `Microsoft.AspNetCore.Http` 2.3.0 from 2018.
  Harmless while inert, but a trap for anyone who later adds a reference.
- **`publish-package.bat` is still stale.** It was copied from an unrelated repository and
  references a solution and projects that do not exist here. Note that every `.bat` file is
  ignored by `.gitignore`, so these are local helpers that never reach other clones. Either
  correct it or delete it.

---

## ❓ Open Questions

Decisions not yet taken. Listed so the gaps are visible rather than implied.

- **Should the PostgreSQL provider offer opt-in schema creation?** It currently never issues
  `CREATE TABLE`. An explicit, opt-in initializer would remove a deployment footgun, but it
  also moves schema ownership away from the migration pipeline. Undecided.
- **Should option binding become configuration-aware?** `RedisOptions.Configuration` is a
  delegate and `PostgreSqlOptions.ConnectionString` is a string, so neither can be populated
  with `Bind`. Registration extensions that accept `IConfiguration` would remove a documented
  sharp edge, at the cost of a new overload.
- **Should the release be unlisted?** Version 2.0.0 is live and its README instructs a call
  that does not compile. NuGet allows a version to be unlisted: it disappears from search
  while existing installs keep working. Whether that is the right call for current users is a
  judgement about them, not a technical question.

---

## 🔌 Future Providers

Not committed. These are the directions under discussion.

- An in-memory provider for single-instance applications and test suites.
- Additional relational providers sharing the PostgreSQL implementation pattern.
- A document-store provider for environments without a relational database.

Adding a provider requires no change to the core package. It implements `IIdempotencyStorage`
and ships independently.

---

## 🚫 Settled

Decisions already taken, recorded so their absence is not read as an oversight.

- **The `Core.Idempotency` namespace root and the `CoreSystem.Idempotency` package id stay
  as they are.** Both are published API. Aligning them would be a breaking change with no
  consumer benefit.
- **One H1 per documentation page.** The existing pages promoted every section to an H1. The
  convention is now a single title with `##` sections below it, and every section heading
  carries an icon.