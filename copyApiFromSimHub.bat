@echo off

set SIMHUB_DIR=\Games\SimHub

del /q SimHub\*

copy "%SIMHUB_DIR%\SimHub.Plugins.dll"   SimHub
copy "%SIMHUB_DIR%\GameReaderCommon.dll" SimHub
copy "%SIMHUB_DIR%\log4net.dll"          SimHub
copy "%SIMHUB_DIR%\Newtonsoft.Json.dll"  SimHub
copy "%SIMHUB_DIR%\MahApps.Metro.dll"                    SimHub
copy "%SIMHUB_DIR%\MahApps.Metro.SimpleChildWindow.dll"  SimHub

copy "%SIMHUB_DIR%\Jint.dll" SimHub
copy "%SIMHUB_DIR%\Acornima.dll" SimHub
copy "%SIMHUB_DIR%\ICSharpCode.AvalonEdit.dll" SimHub
