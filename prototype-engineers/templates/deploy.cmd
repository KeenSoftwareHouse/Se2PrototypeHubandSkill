@echo off
rem Double-click friendly: build + deploy + launch, without PowerShell execution-policy prompts.
rem Pass --no-launch to only build and deploy.
setlocal
set "ARGS="
if /I "%~1"=="--no-launch" set "ARGS=-NoLaunch"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-game.ps1" %ARGS%
if errorlevel 1 pause
