# MEMORY

Working state of CoreSystem.Idempotency. Read this first, then `AGENTS.md` for the permanent rules.
Keep it under ~50 lines. If something becomes a standing rule, move it to `AGENTS.md` and drop it here.
Never store secrets, tokens, or personal data.

## Current state (2026-10-14)

- Published: **2.0.0** on nuget.org for all three packages, alongside 1.0.0–1.0.4.
  2.0.0 shipped documentation that does not compile and a PostgreSQL schema missing
  `state` and `lease_id`. It also records a git commit that does not exist in this repo,
  so the published package cannot be reproduced from source.
- **2.0.1 is prepared but not released**: the three csproj files and the changelog are
  updated in the working tree only. Nothing ships until the version is tagged.
- `main` at `83db38f`. PRs #2 (docs), #3 (metadata and pipeline) and #4 (XML docs and
  SourceLink) are merged. There are still **no git tags** in this repository.
- Five test projects green: 3 unit (core, Redis, PostgreSql) + 2 Testcontainers integration.

## Decisions

- **Core carries no storage code.** Storage sits behind `IIdempotencyStorage`, shipped as separate
  packages. Rationale: a consumer picks Redis or PostgreSQL without dragging in both dependency trees.
- **Lease-based contract** (`TryAcquire` / `Complete` / `Release` / `Get` / `Set`) rather than a plain
  get/set cache. Rationale: concurrent duplicates must be *rejected*, not both executed, so acquire has
  to be atomic. Any change here is breaking for every provider.
- **Namespace `Core.Idempotency.*` vs PackageId `CoreSystem.Idempotency*`.** Looks like a bug, is not.
  Rationale: PackageId is the NuGet contract; the namespace is already public API since 1.x.
- **PostgreSQL DDL is duplicated** in `src/.../scripts/postgres.sql` and `tests/.../Scripts/postgresql.sql`.
  Rationale: consumers read the shipped script without referencing the test project. Cost: always edit
  both, never just one.
- **`src/Directory.Build.props` exists and is scoped to `src/`.** It enables XML docs, SourceLink and
  embedded PDBs. Rationale: consumers need IntelliSense; test projects would only collect CS1591.

## Mistakes to avoid

- **Verify the committed state, not the working tree.** This bit twice. A green local build proved
  nothing, because uncommitted lock files made the tree self-consistent while the commit was not.
  Use `git worktree add --detach <temp> HEAD` and run restore and build there before pushing.
- **Lock files are committed and CI restores `--locked-mode`.** Any package change needs
  `dotnet restore --use-lock-file --force-evaluate` *and* the regenerated files staged in the same
  commit. Regenerate from the committed csproj versions, or the lock files will record a version
  that does not exist yet.
- **`git stash pop` aborts entirely on the first conflict** and leaves a partial application. The
  stash is kept, so recover with `git checkout stash@{0} -- <file>` rather than retrying the pop.
- **`Out-File -Encoding utf8` writes a BOM** in Windows PowerShell 5.1, which silently lands in
  commit messages and files. Use `[System.IO.File]::WriteAllText` with `UTF8Encoding($false)`.
- **PowerShell cannot chain `.Replace()` on a new line.** Assign to a variable first.
- **`git stash push --keep-index`** is useful for building only what a PR stages, but note that
  untracked files are *not* stashed, so an untracked props file can enable settings the build
  otherwise would not have.
- **Published versions are immutable.** Check nuget.org before tagging.
- **Do not fake Redis/PostgreSQL in unit tests.** The Testcontainers integration suites are the
  intended place for storage behavior.

## Known gaps

- `publish-package.bat` is still stale: it references a solution and projects from an unrelated
  repository. All `.bat` files are gitignored, so they are local helpers only.
- About 27 inert central package pins remain, including `xunit` 2.5.3 beside the `xunit.v3`
  actually used, and `Microsoft.AspNetCore.Http` 2.3.0.
- Undecided: whether to unlist 2.0.0 on nuget.org, and whether the PostgreSQL provider should
  ever offer opt-in schema creation.