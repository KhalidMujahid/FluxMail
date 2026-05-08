@echo off
setlocal

echo [1/3] Downloading dependencies...
go mod tidy
if errorlevel 1 (
    echo ERROR: go mod tidy failed. Make sure Go 1.22+ is installed.
    exit /b 1
)

echo [2/3] Building fluxmail.exe (64-bit Windows)...
set GOARCH=amd64
set GOOS=windows
go build -ldflags="-s -w" -o fluxmail.exe .
if errorlevel 1 (
    echo ERROR: build failed.
    exit /b 1
)

echo [3/3] Done!
echo.
echo  fluxmail.exe is ready in: %CD%
echo.
echo  Quick start:
echo    fluxmail --help
echo    fluxmail providers list
echo    fluxmail send --to you@example.com --subject "Hi" --html "<p>Hello!</p>"
echo    fluxmail bulk --file recipients.txt --template 1 --delay 1
echo.
endlocal
