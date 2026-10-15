@echo off
setlocal enabledelayedexpansion

rem ============================================================================
rem  CoreSystem.Idempotency - NuGet lock files
rem
rem  Usage:
rem      generate-lock-files.bat
rem
rem  Regenerates every packages.lock.json in the solution.
rem
rem  Lock files are committed and CI restores with --locked-mode, so any
rem  package change that is not reflected here fails the build on GitHub
rem  rather than locally. Commit the result together with the change that
rem  caused it.
rem
rem  This script does NOT stage or commit anything. Review the diff first.
rem ============================================================================

cd /d "%~dp0"

set "SLN="

rem Resolve the solution: the only .sln at the repository root.
for %%F in (*.sln) do set "SLN=%%F"

if "%SLN%"=="" (
    echo.
    echo ERROR: no solution file found at the repository root.
    pause
    exit /b 1
)

echo ==========================================
echo CoreSystem.Idempotency - NuGet Lock Files
echo ==========================================
echo.
echo Solution: %SLN%
echo.

echo [1/4] Restoring and regenerating lock files...
echo.

dotnet restore "%SLN%" --use-lock-file --force-evaluate

if errorlevel 1 (
    echo.
    echo ERROR: restore failed, so the lock files were not regenerated.
    pause
    exit /b 1
)

echo.
echo [2/4] Lock files in the repository:
echo.

rem 'for /r' treats a non-wildcard set as a directory pattern, so it walks the
rem whole tree instead of matching files. 'dir /s /b' filters by filename.
rem FOUND must be initialised: an unset variable expands to nothing, and
rem '""==0' is false, so the "none found" branch could never be reached.
set "FOUND=0"
for /f "delims=" %%F in ('dir /s /b packages.lock.json 2^>nul') do (
    echo    %%~nxF  -  %%~dpF
    set /a FOUND+=1
)

if !FOUND!==0 (
    echo    (none found)
)

echo.
echo [3/4] Git status for lock files:
echo.

rem A bare 'packages.lock.json' pathspec only matches the repository root,
rem where no lock file exists, so it always reported nothing. The glob is
rem what makes git match them at any depth.
git status --short -- "*packages.lock.json"

if %errorlevel%==128 (
    echo.
    echo ERROR: git status failed. Is this a git repository?
    pause
    exit /b 1
)

echo.
echo [4/4] Verifying lock files are consistent...
echo.

rem The same restore mode CI uses. It fails if a lock file is stale.
dotnet restore "%SLN%" --locked-mode

if errorlevel 1 (
    echo.
    echo ERROR: --locked-mode restore failed. The lock files are not in sync.
    echo Stage the regenerated lock files and re-run.
    pause
    exit /b 1
)

echo.
echo ==========================================
echo Lock files regenerated and verified.
echo ==========================================
echo.
echo Review the changes above, then stage and commit them together with
echo whatever change caused them.
echo.

pause
endlocal