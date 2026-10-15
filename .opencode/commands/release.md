---
description: Publish a CoreSystem component by tagging it (validate, build, test, tag, push)
---

Load the `nuget-release` skill and publish a release.

Target: $ARGUMENTS

Accept two arguments and nothing else:

    release Redis 2.5.1

- `$argumentA` = component without prefix, e.g. `Idempotency`, `Idempotency.Redis`
- `$argumentB` = version, e.g. `2.5.1`

Derive, do not hardcode:

    $NAME    = Core.$argumentA          -> Core.Idempotency        (folder, paths, tag)
    $PACKAGE = CoreSystem.$argumentA    -> CoreSystem.Idempotency  (PackageId, nuspec)
    $TAG     = $NAME/v$argumentB        -> Core.Idempotency/v2.0.1

The `Core.` prefix belongs to paths and tags. The `CoreSystem.` prefix belongs
to the PackageId and the tag message. They are not interchangeable: the folder
is `Core.Idempotency` while the package is `CoreSystem.Idempotency`, and
publish.yml resolves the csproj as `src/$PROJECT/$PROJECT.csproj`, so the tag
must carry the folder name or the workflow fails before packing.

## 1. Discover the solution and projects

Do not assume paths. Resolve them:

- Solution: the only `*.sln` at the root, or the one containing `src/$NAME/`.
  If none matches, ABORT and say which file is missing.
- Production: `src/$NAME/$NAME.csproj`. If absent, ABORT.
- Unit tests: `tests/$NAME.UnitTests/$NAME.UnitTests.csproj`.
- Integration: `tests/$NAME.IntegrationTests/$NAME.IntegrationTests.csproj`.

Derive the test projects by convention rather than by a hardcoded list.
`tests/<X>.UnitTests` and `tests/<X>.IntegrationTests` belong to component `<X>`,
so anything whose prefix does not match `$NAME` is another component's and is
skipped. This is what makes the flow portable: a new component needs no edit.

Validate the association before running anything. Each discovered test project
must reference `src/$NAME/` in its `ProjectReference`. If a project named for
this component points at a different `src/`, ABORT: it would compile the wrong
code and publish it.

Report the discovery before continuing:

    Componente  : <argumentA>
    Solución    : <resolved>          [how it was found]
    PackageId   : <read from csproj>
    Unitarias   : <path>             [run]
    Integración : <path>             [run] | (no existe)
    Tag         : <TAG>

Read the `PackageId` from the csproj. Never construct it from the component
name; the two do not always match.

## 2. Validate the test modules before executing them

```bash
# unitarias — OBLIGATORIAS
if [ ! -f "tests/Core.$argumentA.UnitTests/Core.$argumentA.UnitTests.csproj" ]; then
    echo "ABORTAR: no existe tests/Core.$argumentA.UnitTests"
    exit 1
fi

# integración — OPCIONAL pero visible
if [ -f "tests/Core.$argumentA.IntegrationTests/Core.$argumentA.IntegrationTests.csproj" ]; then
    TEST_INTEGRATION="tests/Core.$argumentA.IntegrationTests/Core.$argumentA.IntegrationTests.csproj"
else
    echo "INFO: $argumentA no tiene modulo de integracion; se publica solo con unitarias"
fi
```

Unit tests missing means ABORT. Integration tests missing means continue and
say so in the summary, because that is the state of the repo rather than an
omission: only `Redis` and `PostgreSql` have integration projects; `Idempotency` itself does not.

## 3. Pre-checks

Confirm the repository is correct, the arguments are valid, the discovered
paths exist, the version is not already published, and the tag does not exist
locally or on the remote.

Do not delete local changes. Do not overwrite existing tags. If the working
tree is dirty, stop and report what is there.

## 4. Restore, clean and build

Restore only the project being published, never the whole solution:

```bash
dotnet restore src/Core.$argumentA/Core.$argumentA.csproj
dotnet clean   src/Core.$argumentA/Core.$argumentA.csproj
dotnet restore src/Core.$argumentA/Core.$argumentA.csproj
dotnet build   src/Core.$argumentA/Core.$argumentA.csproj -c Release --no-restore
```

Do not run `dotnet restore <solution> --force-evaluate` here. Measured on this
repo it leaves lock files intact but rewrites a stale version anchor, for
example `CoreSystem.Idempotency` from `[1.0.6, )` to `[2.0.1, )`. That is a correct
update, not damage, but it is unrelated to the release. If any lock file
changes as a side effect, revert it before committing.

## 5. Vulnerability report — skipped

Do not run `dotnet list package --vulnerable`. It exits 0 even when it reports
a High advisory, in both text and JSON, so it never gates anything and its
output can only mislead. The real gate already ran in the restore step of
section 4: `NuGetAudit` with `NuGetAuditMode=all` plus `TreatWarningsAsErrors`
turns NU1902/NU1903 into a restore failure, and section 4 aborts on a failed
restore. A restore that succeeds is the audit result. Do not add this step back
"just to show the report".

## 6. Tests

```bash
dotnet test tests/Core.$argumentA.UnitTests/Core.$argumentA.UnitTests.csproj \
  -c Release --no-restore

dotnet test "$TEST_INTEGRATION" -c Release --no-restore   # only if it exists
```

Do not continue if the build fails, a test fails, or a required test module
cannot run. Keep the error detail for diagnosis.

## 7. Git

```bash
git checkout main
git pull origin main
```

If the pull changed code relative to what was tested, repeat steps 4, 5 and 6
against the final code before publishing. If local changes could be lost or
there are conflicts, stop and ask.

## 8. Confirm, then tag and publish

Show the component, the version, the resolved solution, the tag, and the build
and test results. Wait for explicit confirmation before any operation that
modifies the repository or pushes. Keep the summary to that: no polling loop,
no repeated re-checking of a value that will not change within the run.

After confirmation:

```bash
git tag -a Core.$argumentA/v$argumentB -m "CoreSystem.$argumentA $argumentB"
git push origin Core.$argumentA/v$argumentB
```

Push only that tag. Then summarise the release.

Do not sleep in a loop polling nuget.org for the new version to appear. The
index lags a publish by several minutes and the workflow result already states
whether the push succeeded. Report what the workflow returned and stop.

Published versions are immutable. `CoreSystem.Idempotency` 2.0.0 is already live, so
it can never be re-pushed; a re-publish attempt fails with 409, which is the
correct outcome. Never publish if a required validation failed or the tag
exists.

## Publish order

Base-first, not reverse:

    Core.Idempotency  ->  Core.Idempotency.Redis, Core.Idempotency.PostgreSql

The providers pin the core through a `PackageReference` on a version that
must already be live, so publishing a dependent first puts a package on the
feed whose dependency does not exist yet.

`publish.yml` has no `--skip-duplicate`, so a partially completed release is
not resumable by re-running the tags that already succeeded. A failure in the
last tag leaves the earlier ones live and the recovery path is manual.
