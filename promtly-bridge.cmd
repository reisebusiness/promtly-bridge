@echo off
setlocal
REM Double-click launcher for the Promtly bridge.
REM
REM This is the ONLY file you need. If promtly-bridge.mjs is not sitting next
REM to it, this fetches it from promtly.dev once and keeps it here - a setup
REM that depends on two downloads landing in the same folder is a setup that
REM fails on the machine you are in a hurry on.
cd /d "%~dp0"
set "SRC=%~dp0promtly-bridge.mjs"

where node >nul 2>nul || (
  echo.
  echo   Node.js is required and is not installed.
  echo   Install the LTS build from https://nodejs.org then run this again.
  echo.
  pause
  exit /b 1
)

if not exist "%SRC%" (
  echo   Fetching the bridge from promtly.dev ...
  curl -fsSL "https://promtly.dev/bridge/promtly-bridge.mjs" -o "%SRC%"
  if errorlevel 1 (
    REM curl -f writes nothing on an HTTP error, but a truncated transfer can
    REM leave a partial file behind. Never keep one - half a bridge would fail
    REM later with a syntax error instead of here with an explanation.
    if exist "%SRC%" del "%SRC%"
    echo.
    echo   Could not download it. Save promtly-bridge.mjs from
    echo   https://promtly.dev/bridge into this folder and run this again.
    echo.
    pause
    exit /b 1
  )
)

node "%SRC%"
pause
