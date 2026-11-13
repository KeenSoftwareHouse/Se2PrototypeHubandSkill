@echo off
rem Double-click to start Space Engineers 2 (Steam) with this plugin.
rem A plugin is unsigned code with full access to your PC: only run it if you trust the sender.
rem Finds the game through Steam automatically. If Steam can't be found, run once in PowerShell:
rem   .\find-game.ps1 -SteamDir "<folder containing steam.exe>"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0launch.ps1"
if errorlevel 1 pause
