# AGENTS.md

Guidance for coding agents working in this repository.

## Scope

- Applies to the whole repository.
- If a subdirectory later adds its own `AGENTS.md`, that file overrides this one for files in that subtree.

## Project Overview

- Repository: `SimHubPropertyServer`
- Main code: `PropertyServer.Plugin/`
- Solution: `SimHubPropertyServer.sln`
- Target framework: `.NET Framework 4.8` (classic csproj, not SDK-style)
- UI stack: WPF/XAML (SimHub plugin UI)

## Build Prerequisites

Before building, required SimHub DLLs must exist in `SimHub/`.

Use the existing helper script:

- `copyApiFromSimHub.bat`

See `doc/Building.adoc` for the expected DLL list.

## Common Commands

- If `msbuild` is not in `PATH`, initialize the Visual Studio build environment first:
  - `call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat" && msbuild -p:Platform="Any CPU" -p:Configuration=Debug`
- Restore packages:
  - `msbuild -t:restore -p:Platform="Any CPU" -p:RestorePackagesConfig=true`
- Build release:
  - `msbuild -p:Platform="Any CPU" -p:Configuration=Release`
- Build debug:
  - `msbuild -p:Platform="Any CPU" -p:Configuration=Debug`

## Coding Conventions

### General

- Always write the simplest, cleanest code possible.
- Code must always be consistent with the rest of the application.
- Never write unnecessary code.
- Remove unused imports.
- Do not introduce new dependencies unless required.
- Keep language version compatibility in mind (`C# 7.3` features are safest here).
- Avoid broad refactors when implementing focused fixes.

### File Headers
- All C# files must include:
  ```csharp
  // Copyright (C) YEAR Martin Renner
  // LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)
  ```
  whereas "YEAR" should be replaced with the current year.

- When modifying an existing file, update the year in the header to the current year.


## UI/WPF Notes

- Existing controls use SimHub style components where available (for example `SHButtonPrimary`).
- Keep behavior consistent with current UX patterns in `PropertyServer.Plugin/PropertyServer/Ui/` and `PropertyServer.Plugin/ComputedProperties/Ui/`.
- Prefer small, targeted XAML/code-behind adjustments over large UI rewrites.

## Validation

- Build after code changes when possible.
- If the repository has unrelated pre-existing build errors, document them and ensure your change does not add new ones.

## Safety Rules

- Never commit secrets or machine-local paths.
- Do not delete user changes you did not create.
- Do not run destructive git operations (`reset --hard`, forced checkout) unless explicitly requested.
