---
description: Publish a CoreSystem component by tagging it (validate, build, test, tag, push)
---

Load the `nuget-release` skill and publish a release.

## Arguments

Accept exactly two arguments: `release <Component> <Version>`, e.g. `release Redis 2.5.1`.

- `argumentA`: component without prefix, e.g. `Idempotency` or `Idempotency.Redis`
- `argumentB`: version, e.g. `2.5.1`
- Derive names; do not hardcode:
  - `NAME = Core.<argumentA>` for folders, paths, and tag
  - `PACKAGE = CoreSystem.<argumentA>` is an example only; read the actual `PackageId` from the `.csproj`
  - `TAG = <NAME>/v<argumentB>`

`Core.` is used for paths/tags; `CoreSystem.` is used for package IDs and tag messages. They are not interchangeable. `publish.yml` resolves the project as `src/$PROJECT/$PROJECT.csproj`, so the tag must use the folder name.

## 1. Discover and validate projects

Resolve the only root `*.sln`, or the solution containing `src/$NAME/`; abort and report the missing file if neither resolves.

Require `src/$NAME/$NAME.csproj`. Discover tests by convention:
- Required unit tests: `tests/$NAME.UnitTests/$NAME.UnitTests.csproj`
- Optional integration tests: `tests/$NAME.IntegrationTests/$NAME.IntegrationTests.csproj`

Do not hardcode projects. Only tests matching `$NAME` belong to this release. Validate each test project's `ProjectReference` points to `src/$NAME/`; otherwise abort.

Read `PackageId` from the production `.csproj`; never infer it from the component name.

Report before continuing: component; resolved solution and how it was found; `PackageId`; unit-test path; integration-test path or that it is absent; tag.

## 2. Pre-checks

Validate repository, arguments, paths, unpublished version, and absence of the tag locally/remotely. Never delete changes or overwrite tags. If the working tree is dirty, stop and report it.

For a provider, also confirm that every `CoreSystem.Idempotency*` dependency recorded in the package is already published. Read the versions from the pinned `PackageVersion` entries or from the built `.nuspec`, then check them against `https://api.nuget.org/v3-flatcontainer/<id>/index.json`. A package that declares an unpublished dependency is unfixable: consumers cannot restore it and the version can never be replaced.

## 3. Sync with origin, then work against that state

```bash
git checkout main
git pull origin main
```

Do this before building or testing, not after. The tag is created from `HEAD`, so the commit that must be validated is the one that ends up published. If `main` is not up to date, the whole validate-then-pull cycle has to be repeated. Stop and ask if local changes may be lost or conflicts occur.

## 4. Restore, clean, and build

Restore/build only the production project, not the whole solution:

```bash
dotnet restore src/Core.$argumentA/Core.$argumentA.csproj
dotnet clean   src/Core.$argumentA/Core.$argumentA.csproj
dotnet restore src/Core.$argumentA/Core.$argumentA.csproj
dotnet build   src/Core.$argumentA/Core.$argumentA.csproj -c Release --no-restore
```

Do not run `dotnet restore <solution> --use-lock-file --force-evaluate`: here it can rewrite version anchors (e.g. `CoreSystem.Idempotency` from `[1.0.6, )` to `[2.0.1, )`), an unrelated change. Revert any lock-file changes caused by this flow.

## 5. Vulnerability gate

Do not use `dotnet list package --vulnerable` as a gate: it can exit 0 despite a High advisory. The configured restore audit (`NuGetAudit`, `NuGetAuditMode=all`, `TreatWarningsAsErrors`) gates NU1902/NU1903. Abort on restore failure; do not add a report-only step.

## 6. Tests

Unit tests are mandatory. Abort if missing, failing, or unable to run:

```bash
dotnet test tests/Core.$argumentA.UnitTests/Core.$argumentA.UnitTests.csproj -c Release
```

The test projects were not restored in step 4, so do not pass `--no-restore` here; an unpinned restore is required and is also why step 7 re-checks the working tree.

Run integration tests if the project exists; otherwise report that it is absent:

```bash
dotnet test "$TEST_INTEGRATION" -c Release
```

Keep failure details for diagnosis. Do not continue after a build or test failure.

## 7. Verify the artifact, then confirm, tag, and publish

Inspect the produced `.nupkg` before tagging. A successful build does not prove the package is correct, and 2.0.0 shipped a README example that did not compile and a schema missing two mandatory columns. Confirm:

- `README.md` and `LICENSE` are at the package root, and the `.nuspec` declares `<readme>README.md</readme>`.
- The `.nuspec` `id` and `version` match the component and the argument.
- Every `CoreSystem.Idempotency*` dependency is already published (step 2).

Then show component, version, solution, tag, and build/test results; wait for explicit confirmation before modifying the repository or pushing.

After confirmation, create and push only the component tag. Use the `PackageId` read in step 1 for the tag message, never a name derived from the component:

```bash
git tag -a Core.$argumentA/v$argumentB -m "$PACKAGE_ID $argumentB"
git push origin Core.$argumentA/v$argumentB
```

Summarize the release. Do not poll NuGet.org; its index may lag, and the workflow result reports push status.

Published versions are immutable. `CoreSystem.Idempotency` `2.0.0` is already live and cannot be republished; a repeat returns HTTP 409. Never publish if validation fails or the tag already exists.

## 8. Release order and partial failures

Publish base packages before providers:
`Core.Idempotency` → `Core.Idempotency.Redis`, `Core.Idempotency.PostgreSql`.

Providers pin the core through a `PackageReference` on a version that must already be live, so publishing a dependent first puts a package on the feed whose dependency does not exist yet. `publish.yml` lacks `--skip-duplicate`; do not rerun successful tags after partial failure. Record successes and recover manually.