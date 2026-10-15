@echo off
setlocal

cd /d "%~dp0"

echo ==========================================
echo CoreSystem.Idempotency - MkDocs
echo ==========================================
echo.

if not exist ".venv\pyvenv.cfg" (
    echo [1/4] Creating Python virtual environment...

    if exist ".venv" (
        echo Removing incomplete virtual environment...
        rmdir /s /q ".venv"
    )

    py -3.11 -m venv .venv

    if errorlevel 1 (
        echo.
        echo ERROR: Could not create the Python virtual environment.
        pause
        exit /b 1
    )
) else (
    echo [1/4] Virtual environment already exists.
)

echo.
echo [2/4] Installing documentation dependencies...

.venv\Scripts\python.exe -m pip install -r requirements.txt

if errorlevel 1 (
    echo.
    echo ERROR: Could not install documentation dependencies.
    pause
    exit /b 1
)

echo.
echo [3/4] Starting MkDocs...
echo.
echo Documentation will be available at:
echo http://127.0.0.1:8000/
echo.
echo Press Ctrl+C to stop the server.
echo.

.venv\Scripts\python.exe -m mkdocs serve

if errorlevel 1 (
    echo.
    echo ERROR: MkDocs stopped with an error.
    pause
    exit /b 1
)

endlocal