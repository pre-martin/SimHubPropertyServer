# Copyright (C) 2026 Martin Renner
# LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

<#
.SYNOPSIS
    Packages the SimHub API DLLs (as copied by copyApiFromSimHub.bat into .\SimHub)
    and publishes them as a GitHub release asset to the private
    "SimHubPropertyServer-ci-deps" repo, so that GitHub Actions builds use the
    exact same DLLs as the local build.

.PARAMETER Tag
    Release tag to create in the deps repo, e.g. the SimHub version (e.g. "2025.10.03").

.PARAMETER Token
    A fine-grained GitHub PAT with "Contents: Read and write" on the deps repo.
    Falls back to $env:SIMHUB_DEPS_PAT, then to the content of
    tools\publishSimHubDeps.token.txt (git-ignored), if not given.

.PARAMETER Repo
    The deps repo in "owner/name" form. Defaults to pre-martin/SimHubPropertyServer-ci-deps.

.EXAMPLE
    .\tools\publishSimHubDeps.ps1 -Tag 2025.10.03
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Tag,

    [string]$Token = $env:SIMHUB_DEPS_PAT,

    [string]$Repo = "pre-martin/SimHubPropertyServer-ci-deps"
)

$ErrorActionPreference = "Stop"

if (-not $Token) {
    $tokenFile = Join-Path $PSScriptRoot "publishSimHubDeps.token.txt"
    if (Test-Path $tokenFile) {
        $Token = (Get-Content -Path $tokenFile -Raw).Trim()
    }
}

if (-not $Token) {
    throw "No GitHub token given. Pass -Token, set `$env:SIMHUB_DEPS_PAT, or " +
          "create tools\publishSimHubDeps.token.txt with the token."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$simHubDir = Join-Path $repoRoot "SimHub"

if (-not (Test-Path $simHubDir)) {
    throw "SimHub\ not found. Run copyApiFromSimHub.bat first."
}

$zipPath = Join-Path $repoRoot "simhub-dlls.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath
}
Compress-Archive -Path (Join-Path $simHubDir "*.dll") -DestinationPath $zipPath

$headers = @{
    Authorization = "Bearer $Token"
    Accept        = "application/vnd.github+json"
}

Write-Host "Creating release '$Tag' in $Repo ..."
$body = @{
    tag_name = $Tag
    name     = $Tag
} | ConvertTo-Json

$release = Invoke-RestMethod -Headers $headers -Method Post `
    -Uri "https://api.github.com/repos/$Repo/releases" -Body $body

$uploadUrl = $release.upload_url -replace '\{.*\}', ''
$uploadHeaders = $headers.Clone()
$uploadHeaders["Content-Type"] = "application/zip"

Write-Host "Uploading simhub-dlls.zip ..."
Invoke-RestMethod -Headers $uploadHeaders -Method Post `
    -Uri "$($uploadUrl)?name=simhub-dlls.zip" -InFile $zipPath | Out-Null

Remove-Item $zipPath

Write-Host "Done. CI will pick this up as the latest release of $Repo."
