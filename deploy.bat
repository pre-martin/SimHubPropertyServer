@echo off
setlocal

set SCRIPT_DIR=%~dp0
pushd "%SCRIPT_DIR%"

rem Script to deploy locally.
rem If SimHub is started with admin privileges, the script has to be started as admin, too.

set CONFIG=Release
if "%1%" == "debug" set CONFIG=Debug

echo.
echo Building for configuration: %CONFIG%
echo.

dotnet build -c %CONFIG% SimHubPropertyServer.sln
if %errorlevel% neq 0 (
    popd
    exit /b 1
)

taskkill /im SimHubWPF.exe /t /f
timeout /t 1

copy /y PropertyServer.Plugin\bin\%CONFIG%\net48\PropertyServer.dll \Games\SimHub\

start /d \Games\SimHub SimHubWPF.exe

popd
