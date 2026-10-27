# MEMORY

Working state of CoreSystem.Idempotency. Read this first, then `AGENTS.md` for the permanent rules.
Keep it under ~50 lines. If something becomes a standing rule, move it to `AGENTS.md` and drop it here.
Never store secrets, tokens, or personal data.

## Current state (2026-10-26)

- **In progress: migration to .NET 10 SDK with multi-targeting `net8.0;net9.0;net10.0`.**
  `global.json` pins SDK 10.0.201 (`rollForward: latestFeature`) and
  `Microsoft.Testing.Platform`; `dotnet test` now needs `--project <csproj>`.
  Build and the three unit suites are green with `TreatWarningsAsErrors` on.
- A **root `Directory.Build.props`** now exists (TFMs, quality gates, NuGetAudit,
  locked restore). MSBuild only auto-imports the nearest props file, so `src/` and
  `tests/` props import it explicitly; `tests/` adds `GenerateDocumentationFile=false`
  and `NoWarn` for CA1707, CA1001 and (net10.0 only) ASPDEPR004/ASPDEPR008.
- The providers currently reference core via `ProjectReference` (staged, uncommitted).
  This **contradicts the published-package decision below** and must be settled before
  cutting any release, otherwise provider nuspecs lose the `CoreSystem.Idempotency` dependency.
- Published on nuget.org: all three at **2.0.1** (core, Redis, PostgreSql).
- **Core `CoreSystem.Idempotency` 2.0.2 is live on nuget.org** (PR #15, tag `Core.Idempotency/v2.0.2`).
- Branch `chore/pin-core-2.0.2` raises the central pin to the core 2.0.2 and regenerates the lock
  files. The two provider packages declare `CoreSystem.Idempotency >= 2.0.2` once it is merged.
- Still pending: tag and publish the providers, in this order —
  `Core.Idempotency.Redis/v2.0.2`, then `Core.Idempotency.PostgreSql/v2.0.2`.
- Branch `release/2.0.1-providers` is what established the pinned core dependency; that
  decision is settled and a `ProjectReference` revert must not come back.
- `main` was at `c3888d3` (PR #13) when 2.0.2 was prepared.
- **2.0.1 is fully released**: core, Redis and PostgreSql all live on nuget.org, tagged
  `Core.Idempotency/v2.0.1`, `Core.Idempotency.Redis/v2.0.1`, `Core.Idempotency.PostgreSql/v2.0.1`.
  `main` at `dcc6ce5` (PR #10).
- Docker Desktop is not started by default here; the integration suites need it and
  fail with `DockerUnavailableException` until it is running.
- **Releases are run by hand with `publish-package.bat`, no agent, no `/release` command.**
  The `nuget-release` skill and `release.md` were deleted: with the script performing every
  gate, an agent only re-derived mechanical decisions for ~2.3k tokens of repeated
  instructions per run, and two files that describe the process drift from the process.
  Reserve an agent for the judgement calls: which version to cut, interpreting a failed
  gate, and whether to unlist 2.0.0. Those rules live in `AGENTS.md`, not in a skill.
- **`publish.yml` never inspects the `.nupkg`.** It verifies restore `--locked-mode`, build, tests,
  that pack produced exactly one file with the expected name, and gets a 409 on a duplicate
  version. It does not check README, LICENSE, the nuspec `<readme>` declaration or whether the
  pinned dependency exists. So the local script is the only gate against shipping a package like
  2.0.0, whose defects were all invisible to a green CI run. Treat it as production code even
  though it is gitignored.

## Decisions

- **Core carries no storage code.** Storage sits behind `IIdempotencyStorage`, shipped as separate
  packages. Rationale: a consumer picks Redis or PostgreSQL without dragging in both dependency trees.
- **Lease-based contract** (`TryAcquire` / `Complete` / `Release` / `Get` / `Set`) rather than a plain
  get/set cache. Rationale: concurrent duplicates must be *rejected*, not both executed, so acquire has
  to be atomic. Any change here is breaking for every provider.
- **Providers consume the core as a published package, not as a project.** Pinned to 2.0.1
  in `Directory.Packages.props`. Rationale: each provider can then ship on its own tag with a
  concrete nuspec dependency, and the publish workflow restores only the provider csproj.
  Cost: the pinned version must already be live before a provider is tagged, and local
  provider builds test the *published* core rather than the local one.
- **Namespace `Core.Idempotency.*` vs PackageId `CoreSystem.Idempotency*`.** Looks like a bug, is not.
  Rationale: PackageId is the NuGet contract; the namespace is already public API since 1.x.
- **PostgreSQL DDL is duplicated** in `src/.../scripts/postgres.sql` and `tests/.../Scripts/postgresql.sql`.
  Rationale: consumers read the shipped script without referencing the test project. Cost: always edit
  both, never just one.
- **`src/Directory.Build.props` exists and is scoped to `src/`.** It enables XML docs, SourceLink and
  embedded PDBs. Rationale: consumers need IntelliSense; test projects would only collect CS1591.
  Since the .NET 10 migration it imports the root `Directory.Build.props`, which holds the shared
  TFMs and quality gates; do not duplicate settings in both.

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
- **A transitive pin is a floor, not a preference.** `CoreSystem.Redis` 1.0.4 raised its own
  `StackExchange.Redis` floor to 2.13.17 and the central pin had to move with it, or NU1109
  blocks the restore. When a package raises a transitive floor, raise the pin in the same commit.
- **The core pin must move in its own PR, after the core is published.** Raising it in the same
  PR as the version bump deadlocks: `--locked-mode` asks nuget.org for a version that does not
  exist, CI goes red, the core never publishes, the pin can never be satisfied. This is why the
  2.0.2 release took three PRs (#14 providers, #15 core, #16 pin).
- **The checkout moves on its own when a PR merges mid-session.** Check
  `git branch --show-current` immediately before every commit. PR #14 merged while work was in
  flight, the checkout moved to `main`, and a follow-up commit landed there. `main` is protected
  so the push failed, but the commit existed locally on `main`. Recovery:
  `git branch <new> <sha>` → `git reset --hard origin/main` → `git checkout <new>`.
- **Published versions are immutable.** Check nuget.org before tagging. 2.0.1 was already live
  for all three packages when 2.0.2 was cut, so the csproj version had to move first.
- **Do not fake Redis/PostgreSQL in unit tests.** The Testcontainers integration suites are the
  intended place for storage behavior.
- **A `null` `PackageVersion` audit is not evidence of an unused pin.** `Microsoft.SourceLink.GitHub`
  looked unused because it is referenced from `src/Directory.Build.props`, not from a csproj. Scan
  the props files too before calling a pin inert.

## Known gaps

- `publish-package.bat` and `generate-lock-files.bat` are local helpers only: they are gitignored,
  so they never reach another clone and their fixes are invisible to CI. Review them by reading
  them; the repository rules they implement live in `AGENTS.md` and `.opencode/`.
- Exactly one pin has no direct `PackageReference`: `StackExchange.Redis`. It is not inert, it
  only participates as a `CentralTransitive` floor. `CoreSystem.Redis` 1.0.4 (published
  2026-10-18) raised its own floor to `StackExchange.Redis >= 2.13.17`, so the pin had to move
  with it or NU1109 blocks the restore. Do not trust the old count of ~27 pins; it was stale.
  19 pins are declared, 18 have a direct reference.
- Undecided: whether to unlist 2.0.0 on nuget.org, and whether the PostgreSQL provider should
  ever offer opt-in schema creation.