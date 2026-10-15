@echo off
setlocal enabledelayedexpansion

rem ============================================================================
rem  CoreSystem.Idempotency - Publish a single package
rem ============================================================================
rem
rem  Usage:
rem      publish-package.bat <project> <version>
rem
rem    <project>   folder name under src\, one of:
rem                    Core.Idempotency
rem                    Core.Idempotency.Redis
rem                    Core.Idempotency.PostgreSql
rem
rem    <version>   version to publish, e.g. 2.0.1. Must already be present in the
rem                 csproj; the tag is <project>/v<version> and publish.yml
rem                 re-reads the version from the tag.
rem
rem  Run it from a terminal. It does not pause on exit, so a double-click
rem  closes the window before the result can be read.
rem
rem  This script does NOT publish. It verifies, then creates and pushes the
rem  tag, which is what triggers .github/workflows/publish.yml. Publishing is
rem  done by CI so it runs on ubuntu, not on this machine.
rem
rem  Published versions are immutable: a version already on nuget.org can never
rem  be replaced, and publish.yml has no --skip-duplicate, so a re-run of a
rem  live version fails with 409 instead of pretending to succeed. Check the
rem  version before running this.
rem ============================================================================

cd /d "%~dp0"

set "PROJECT=%~1"
set "VERSION=%~2"

echo ==========================================
echo CoreSystem.Idempotency - Publish
echo ==========================================
echo.

REM ---------------------------------------------------------------------------
REM  Validate arguments
REM ---------------------------------------------------------------------------
if "%PROJECT%"=="" (
    echo ERROR: missing project.
    echo.
    echo Usage: publish-package.bat ^<project^> ^<version^>
    echo.
    echo Projects: Core.Idempotency, Core.Idempotency.Redis, Core.Idempotency.PostgreSql
    goto :fail
)

if "%VERSION%"=="" (
    echo ERROR: missing version.
    echo.
    echo Usage: publish-package.bat ^<project^> ^<version^>
    echo.
    echo Example: publish-package.bat Core.Idempotency 2.0.1
    goto :fail
)

powershell -NoProfile -Command "if ('%VERSION%' -match '^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$') { exit 0 } else { exit 1 }"
if errorlevel 1 (
    echo ERROR: invalid version '%VERSION%'. Use SemVer such as 2.0.1 or 2.0.1-rc.1.
    goto :fail
)

set "CSPROJ=src\%PROJECT%\%PROJECT%.csproj"

if not exist "%CSPROJ%" (
    echo ERROR: '%CSPROJ%' does not exist.
    echo.
    echo The project argument is the folder name under src\, not the PackageId.
    echo Valid values: Core.Idempotency, Core.Idempotency.Redis, Core.Idempotency.PostgreSql
    goto :fail
)

REM ---------------------------------------------------------------------------
REM  Derive the test projects from the project name
REM ---------------------------------------------------------------------------
REM  tests/<X>.UnitTests and tests/<X>.IntegrationTests belong to component <X>,
REM  so a new package needs no edit here. Do not reintroduce a hardcoded list:
REM  this file and publish.yml each carried one and they drifted apart.

set "TESTS=tests\%PROJECT%.UnitTests\%PROJECT%.UnitTests.csproj"
set "INTEGRATION_TESTS=tests\%PROJECT%.IntegrationTests\%PROJECT%.IntegrationTests.csproj"

if not exist "%TESTS%" (
    echo.
    echo ERROR: no unit test project for '%PROJECT%'.
    echo.
    echo Expected: %TESTS%
    echo Unit tests are mandatory for a release. Create or rename it first.
    goto :fail
)

REM Only Redis and PostgreSql have an integration project. Its absence is the
REM state of the repository, not a failure.
if not exist "%INTEGRATION_TESTS%" set "INTEGRATION_TESTS="

REM Read PackageId from the project instead of deriving it from the folder name.
set "PACKAGE_ID="
for /f "tokens=*" %%A in ('powershell -NoProfile -Command "$p=[xml](Get-Content -Raw '%CSPROJ%'); $id=$p.Project.PropertyGroup.PackageId | Where-Object { $_ } | Select-Object -First 1; if ($id) { $id }"') do set "PACKAGE_ID=%%A"
if not defined PACKAGE_ID (
    echo ERROR: PackageId is missing from '%CSPROJ%'.
    echo Add an explicit PackageId before publishing.
    goto :fail
)

echo Project    : %PROJECT%
echo PackageId  : %PACKAGE_ID%
echo Version    : %VERSION%
echo Tag        : %PROJECT%/v%VERSION%
echo.

REM ---------------------------------------------------------------------------
REM  Verify the version in the csproj matches the argument
REM ---------------------------------------------------------------------------
set "CSPROJ_VERSION="
for /f "tokens=*" %%A in ('powershell -NoProfile -Command "([xml](Get-Content -Raw '%CSPROJ%')).Project.PropertyGroup.Version"') do (
    set "CSPROJ_VERSION=%%A"
)

if not "!CSPROJ_VERSION!"=="%VERSION%" (
    echo ERROR: version mismatch.
    echo   argument      : %VERSION%
    echo   csproj        : !CSPROJ_VERSION!
    echo.
    echo The csproj must already carry the version being published, because the
    echo package metadata ships inside the .nupkg. Update Version,
    echo AssemblyVersion, FileVersion and InformationalVersion first.
    goto :fail
)

echo [ok] csproj version matches.

REM ---------------------------------------------------------------------------
REM  Verify the version is not already published
REM ---------------------------------------------------------------------------
echo.
echo [1/8] Checking nuget.org for an existing %PACKAGE_ID% %VERSION%...

set "VERSION_CHECK="
for /f "usebackq delims=" %%V in (`powershell -NoProfile -Command "$ErrorActionPreference='Stop'; try { $id='%PACKAGE_ID%'.ToLowerInvariant(); $r=Invoke-RestMethod ('https://api.nuget.org/v3-flatcontainer/'+$id+'/index.json') -TimeoutSec 20; if ($r.versions -contains '%VERSION%') { 'PUBLISHED' } else { 'NOT_PUBLISHED' } } catch { 'CHECK_FAILED' }"`) do set "VERSION_CHECK=%%V"

if /i "!VERSION_CHECK!"=="PUBLISHED" (
    echo.
    echo ERROR: %PACKAGE_ID% %VERSION% is ALREADY PUBLISHED.
    echo.
    echo Published versions are immutable. Choose a new version.
    goto :fail
)

if /i not "!VERSION_CHECK!"=="NOT_PUBLISHED" (
    echo.
    echo ERROR: could not verify the version on nuget.org.
    echo Check network access and retry before publishing.
    goto :fail
)

echo       not published, OK.

REM ---------------------------------------------------------------------------
REM  Verify the tag does not already exist
REM ---------------------------------------------------------------------------
echo.
echo [2/8] Checking for an existing tag %PROJECT%/v%VERSION%...

set "TAG=%PROJECT%/v%VERSION%"
git show-ref --verify --quiet "refs/tags/%TAG%"
if not errorlevel 1 (
    echo.
    echo ERROR: local tag '%TAG%' already exists.
    goto :fail
)

git ls-remote --tags origin "refs/tags/%TAG%" "refs/tags/%TAG%^{}" > "%TEMP%\publish-remote-tag.txt" 2>nul
if errorlevel 1 (
    echo.
    echo ERROR: could not check remote tags on origin.
    echo Check Git credentials and network access, then retry.
    goto :fail
)

for %%F in ("%TEMP%\publish-remote-tag.txt") do if %%~zF GTR 0 (
    echo.
    echo ERROR: remote tag '%TAG%' already exists on origin.
    type "%TEMP%\publish-remote-tag.txt"
    goto :fail
)

echo       no local or remote tag, OK.

REM ---------------------------------------------------------------------------
REM  Verify the working tree and branch
REM ---------------------------------------------------------------------------
echo.
echo [3/8] Verifying the working tree...

set "DIRTY="
git status --porcelain > "%TEMP%\publish-status.txt" 2>nul
for /f "usebackq delims=" %%S in ("%TEMP%\publish-status.txt") do set "DIRTY=%%S"

if not "!DIRTY!"=="" (
    echo.
    echo ERROR: the working tree is not clean.
    echo.
    git status --short
    goto :fail
)

echo       clean, OK.

set "BRANCH="
for /f "usebackq delims=" %%B in (`git rev-parse --abbrev-ref HEAD`) do set "BRANCH=%%B"

if /i not "!BRANCH!"=="main" (
    echo.
    echo ERROR: you are on branch '!BRANCH!', not main.
    echo The tag must point at a merged state.
    goto :fail
)

echo       on main, OK.

REM ---------------------------------------------------------------------------
REM  Sync with origin
REM ---------------------------------------------------------------------------
echo.
echo [4/8] Syncing with origin...

git pull origin main
if errorlevel 1 goto :fail

REM ---------------------------------------------------------------------------
REM  Restore, clean and rebuild the single project
REM ---------------------------------------------------------------------------
echo.
echo [5/8] Restoring and rebuilding %PROJECT%...

dotnet restore "%CSPROJ%"
if errorlevel 1 goto :fail

dotnet clean "%CSPROJ%" -c Release
if errorlevel 1 goto :fail

dotnet restore "%CSPROJ%"
if errorlevel 1 goto :fail

dotnet build "%CSPROJ%" -c Release --no-restore
if errorlevel 1 goto :fail

REM ---------------------------------------------------------------------------
REM  Run the tests for this package
REM ---------------------------------------------------------------------------
echo.
echo [6/8] Running tests...

echo.
echo       %TESTS%
dotnet test "%TESTS%" -c Release
if errorlevel 1 goto :fail

if defined INTEGRATION_TESTS (
    echo.
    echo       !INTEGRATION_TESTS!
    dotnet test "!INTEGRATION_TESTS!" -c Release
    if errorlevel 1 goto :fail
) else (
    echo.
    echo       no integration test project for this component; skipping.
)

REM ---------------------------------------------------------------------------
REM  Re-check the working tree
REM ---------------------------------------------------------------------------
REM  The test run restores the test projects without --locked-mode, so it can
REM  rewrite their packages.lock.json. The tag points at HEAD, which means the
REM  published package is unaffected, but the release must not end with a tree
REM  it modified: the next run would abort at step 3 with a confusing message.

echo.
echo       re-checking the working tree after the test run...

set "DIRTY_AFTER="
git status --porcelain > "%TEMP%\publish-status-after.txt" 2>nul
for /f "usebackq delims=" %%S in ("%TEMP%\publish-status-after.txt") do set "DIRTY_AFTER=%%S"

if not "!DIRTY_AFTER!"=="" (
    echo.
    echo ERROR: the test run left changes in the working tree.
    echo.
    git status --short
    echo.
    echo Review and commit or discard them before publishing. No tag was pushed.
    goto :fail
)

echo       still clean, OK.


REM ---------------------------------------------------------------------------
REM  Confirm the package was produced with the expected contents
REM ---------------------------------------------------------------------------
echo.
echo [7/8] Verifying the built package...

set "NUPKG=src\%PROJECT%\bin\Release\%PACKAGE_ID%.%VERSION%.nupkg"

if not exist "%NUPKG%" (
    echo.
    echo ERROR: expected '%NUPKG%' but it was not produced.
    dir /b "src\%PROJECT%\bin\Release\*.nupkg"
    goto :fail
)

echo       %NUPKG% found.

REM ---------------------------------------------------------------------------
REM  Verify contents and dependencies, not just the file's existence
REM ---------------------------------------------------------------------------
REM  2.0.0 shipped a README example that did not compile and a PostgreSQL
REM  schema missing two mandatory columns. Both compiled fine, so a successful
REM  build proves nothing about what consumers receive. This check is also the
REM  last gate on a provider pinning a core version that must already be live.

echo       verifying contents and published dependencies...

set "VERIFY="
for /f "usebackq delims=" %%V in (`powershell -NoProfile -Command "$ErrorActionPreference='Stop'; try { Add-Type -AssemblyName System.IO.Compression.FileSystem; $z=[IO.Compression.ZipFile]::OpenRead('%NUPKG%'); try { $names=@($z.Entries.ForEach({$_.FullName})); $e=@($z.Entries.Where({$_.FullName -like '*.nuspec'}))[0]; $sr=New-Object IO.StreamReader($e.Open()); $x=[xml]$sr.ReadToEnd(); $sr.Close(); $m=$x.package.metadata; $err=''; if ($names -notcontains 'README.md') { $err='README.md missing from package root; ' }; if ($names -notcontains 'LICENSE') { $err=$err+'LICENSE missing from package; ' }; if ($m.readme -ne 'README.md') { $err=$err+'nuspec does not declare readme=README.md; ' }; if ($m.id -ne '%PACKAGE_ID%') { $err=$err+'unexpected PackageId: '+$m.id+'; ' }; if ($m.version -ne '%VERSION%') { $err=$err+'unexpected version: '+$m.version+'; ' }; foreach ($g in @($m.dependencies.group)) { foreach ($d in @($g.dependency)) { if (($d.id -like 'CoreSystem.Idempotency*') -and ($d.id -ne '%PACKAGE_ID%')) { $lv=[string]$d.version; try { $r=Invoke-RestMethod ('https://api.nuget.org/v3-flatcontainer/'+$d.id.ToLowerInvariant()+'/index.json') -TimeoutSec 20; if (-not ($r.versions -contains $lv)) { $err=$err+'unpublished dependency: '+$d.id+' '+$lv+'; ' } } catch { $err=$err+'could not verify '+$d.id+' '+$lv+'; ' } } } }; if ($err -ne '') { 'FAIL: '+$err } else { 'OK' } } finally { $z.Dispose() } } catch { 'FAIL: could not read the package: '+$_.Exception.Message }"`) do set "VERIFY=%%V"

if "!VERIFY!"=="OK" (
    echo       contents and dependencies verified, OK.
) else (
    echo.
    echo ERROR: package verification failed.
    echo.
    echo        !VERIFY!
    echo.
    echo Nothing was pushed. Publishing an immutable package with an unpublished
    echo dependency, or with a README or schema that does not work, cannot be
    echo undone. Fix it and publish a new version.
    goto :fail
)

REM ---------------------------------------------------------------------------
REM  Create and push the tag, which triggers publish.yml
REM ---------------------------------------------------------------------------
echo.
echo [8/8] Creating and pushing the tag...

echo.
echo About to create and push this tag:
echo   Project : %PROJECT%
echo   Package : %PACKAGE_ID%
echo   Version : %VERSION%
echo   Tag     : %TAG%
echo.
set "CONFIRM="
set /p "CONFIRM=Type YES to create and push the tag: "
if /i not "!CONFIRM!"=="YES" (
    echo.
    echo Cancelled. No tag was created or pushed.
    goto :fail
)

git tag -a "!TAG!" -m "%PACKAGE_ID% %VERSION%"

if errorlevel 1 (
    echo.
    echo ERROR: failed to create the tag.
    goto :fail
)

git push origin "!TAG!"
if errorlevel 1 (
    echo.
    echo ERROR: failed to push the tag. The local tag was kept.
    echo Do not rerun blindly. Check origin, then retry with:
    echo   git push origin "!TAG!"
    goto :fail_tag_created
)

echo.
echo ==========================================
echo Tag pushed: !TAG!
echo ==========================================
echo.
echo publish.yml is now running. Watch it with:
echo.
echo   gh run watch
echo.
echo Publish order is base-first, not reverse:
echo   Core.Idempotency -^> Core.Idempotency.Redis, Core.Idempotency.PostgreSql
echo.
echo Providers pin the core through a PackageReference on a version that must
echo already be live, so publishing a dependent first would put a package on
echo the feed whose dependency does not exist yet.
echo.
echo The release is not resumable: publish.yml has no --skip-duplicate, so
echo re-running a tag that already published fails with 409 rather than
echo reporting success. A failure in the last tag leaves the earlier ones
echo live, and the recovery path is manual.
echo.
exit /b 0

:fail
echo.
echo ==========================================
echo ABORTED - no tag was pushed.
echo ==========================================
echo.
exit /b 1

:fail_tag_created
echo.
echo ==========================================
echo FAILED - local tag exists; push did not succeed.
echo ==========================================
echo.
exit /b 1
