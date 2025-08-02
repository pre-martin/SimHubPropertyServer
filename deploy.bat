@echo off
setlocal

rem Script to deploy locally.
rem If SimHub is started with admin privileges, the script has to be started as admin, too.

set CONFIG=Release
if "%1%" == "debug" set CONFIG=Debug

echo.
echo Building for configuration: %CONFIG%
echo.

"C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin\amd64\MSBuild.exe" -p:Configuration=%CONFIG% SimHubPropertyServer.sln
if %errorlevel% neq 0 exit /b 1

taskkill /im SimHubWPF.exe /t /f
timeout /t 1

copy /y PropertyServer.Plugin\bin\%CONFIG%\PropertyServer.dll \Games\SimHub\

start /d \Games\SimHub SimHubWPF.exe
