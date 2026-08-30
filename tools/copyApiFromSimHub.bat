@echo off
setlocal

set "SIMHUB_DIR=\Games\SimHub"
set "SCRIPT_DIR=%~dp0"
pushd "%SCRIPT_DIR%"
cd ..

if not exist "%SIMHUB_DIR%\SimHub.Plugins.dll" (
    echo ERROR: SimHub directory not found or incomplete: "%SIMHUB_DIR%"
    popd
    exit /b 1
)

if not exist "SimHub\" (
    mkdir "SimHub"
)

del /q "SimHub\*"

copy "%SIMHUB_DIR%\WoteverLocalization.dll" "SimHub\"
copy "%SIMHUB_DIR%\SimHub.Plugins.dll" "SimHub\"
copy "%SIMHUB_DIR%\GameReaderCommon.dll" "SimHub\"
copy "%SIMHUB_DIR%\log4net.dll" "SimHub\"
copy "%SIMHUB_DIR%\Newtonsoft.Json.dll" "SimHub\"
copy "%SIMHUB_DIR%\MahApps.Metro.dll" "SimHub\"
copy "%SIMHUB_DIR%\MahApps.Metro.SimpleChildWindow.dll" "SimHub\"

copy "%SIMHUB_DIR%\Jint.dll" "SimHub\"
copy "%SIMHUB_DIR%\Acornima.dll" "SimHub\"
copy "%SIMHUB_DIR%\ICSharpCode.AvalonEdit.dll" "SimHub\"

popd
