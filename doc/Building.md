# Building

1. The project requires .NET Framework 4.8, because SimHub is built with this framework.
2. Copy required DLLs from SimHub to the local directory `SimHub`
    - SimHub.Plugins.dll
    - GameReaderCommon.dll
    - log4net.dll
    - Newtonsoft.Json.dll
3. Restore NuGet packages:  
   `msbuild -t:restore -p:Platform="Any CPU" -p:RestorePackagesConfig=true`
4. Build the project:  
   `msbuild -p:Platform="Any CPU" -p:Configuration=Release`
