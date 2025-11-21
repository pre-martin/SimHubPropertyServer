# Copyright (C) 2025 Martin Renner
# LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

# Include as "Before launch" task into IDE. For example:
# - Tool: pwsh
# - Args: -NoProfile -File "$ProjectFileDir$/scripts/CopyToSimHub.ps1" -Configuration "$ConfigurationName$"

param(
    [string]$Configuration = "Debug",
    [string]$TargetDir = "/Games/SimHub"
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = (Get-Item $ScriptDir).Parent
$SourceDll = Join-Path $ProjectDir "PropertyServer.Plugin/bin/$Configuration/PropertyServer.dll"

if (-not (Test-Path $TargetDir)) {
    Write-Host "Target directory $TargetDir does not exist."
    Exit 1
}

try {
    Copy-Item -Path $SourceDll -Destination $TargetDir -Force
    Write-Host "Copied $SourceDll to $TargetDir"
}
catch {
    Write-Error $_
    Exit 1
}

Exit 0
