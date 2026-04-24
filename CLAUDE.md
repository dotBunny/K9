# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

K9 is a collection of small, single-purpose .NET CLI executables that augment game-development automation (heavily oriented around Unreal Engine and Perforce workspaces). Each application under `Applications/` is a self-contained `Exe` project; shared functionality lives in `Shared/` class libraries.

- Executables target `net10.0` with `<LangVersion>14</LangVersion>`.
- Shared libraries (`K9.Core`, `K9.Services.*`, `K9.Unreal`) target `netstandard2.1`.
- `global.json` pins the SDK to version 5.0 with `rollForward: latestMajor` — **.NET 10.0 SDK is required** (see README).

## Build & Run

Build the whole solution (from repo root):

```
dotnet build K9.sln -c Debug
```

Build a single project:

```
dotnet build Applications/K9.OS.CleanFolder/K9.OS.CleanFolder.csproj -c Debug
```

Run an application directly after build (binaries live in each project's `bin/<Config>/`):

```
dotnet Applications/K9.OS.CleanFolder/bin/Debug/K9.OS.CleanFolder.dll ---TARGET=C:\some\path
```

There is no test project and no linter beyond the compiler / `.editorconfig`.

### Configurations (important)

The solution defines three configurations: **Debug**, **Release**, **Workspace**.

- `Debug` / `Release`: standard per-project `bin/<Config>` output.
- `Workspace`: overrides `OutputPath` to `..\..\..\Binaries` (relative to each application project), i.e. a single aggregated `Binaries/` folder at the repo parent. This is the layout `K9.Workspace.Bootstrap` expects when the repo has been cloned into `<workspace>/K9/Source`.
- **Always develop in `Debug`.** The README warns that `K9.Workspace.Bootstrap` and `K9.Workspace.Setup` perform destructive actions (e.g. wiping `\K9\Source\`) when run under `Release`/`Workspace`.

Every `.csproj` runs `git rev-parse HEAD` as a PreBuild target to embed the commit SHA into `SourceRevisionId`; `ConsoleApplication` reads this back via `FileVersionInfo` to print version headers, so builds outside a git checkout will show `Unknown`.

## Architecture

### `ConsoleApplication` + `ProgramProvider` pattern

Every executable follows the same skeleton (see `Applications/K9.OS.CleanFolder/Program.cs` for a canonical example):

1. Instantiate `ConsoleApplication` (from `K9.Core`) with a `ConsoleApplicationSettings` (log category, outputs, pause/header/runtime flags) and a project-specific subclass of `ProgramProvider`.
2. `ConsoleApplication`'s constructor wires built-in modules (`ArgumentsModule`, `AssemblyModule`, `EnvironmentModule`, `PlatformModule`), validates arguments via `ProgramProvider.IsValid`, and auto-routes `HELP` / bad-args to `OutputHelp()`.
3. If `ConsoleApplicationSettings.RequiresElevatedAccess` is set and the process is not elevated, it self-relaunches through `ProcessUtil.Elevate` and exits.
4. The `Program.Main` body does the actual work, then `framework.Shutdown()` (via `using`) flushes logs, prints runtime, and calls `Environment.Exit` with the tracked exit code.

A new application should: add a `csproj` in `Applications/`, reference `Shared/K9.Core` (plus any `Shared/K9.Services.*` needed), subclass `ProgramProvider` for args/help, and mirror the `Program.cs` skeleton above. Register it in `K9.sln` if working in the IDE.

### Argument convention

`ArgumentsModule` parses `Environment.GetCommandLineArgs()` into two buckets:

- **Base arguments**: bare tokens (e.g. `HELP`, `NO-PAUSE`, `QUIET`) — checked with `HasBaseArgument("KEY")` (case-insensitive).
- **Override arguments**: prefixed with `---`, optional `=VALUE` (e.g. `---TARGET=C:\foo` or bare `---FLAG`). Queried via `HasOverrideArgument` / `GetOverrideArgument`.

The `---` triple-dash prefix is load-bearing — do not switch to `--`. Quoted arguments split by the shell are re-joined by `ArgumentsModule` so a quoted path with spaces arrives as a single argument.

### Shared services

- `K9.Core` — foundation (logging, argument parsing, process utilities, file helpers, `SettingsProvider`, `WorkspaceUtil`). Pure `netstandard2.1` so it can be consumed anywhere.
- `K9.Services.Perforce` — wraps `p4` CLI (`PerforceProvider`, `PerforceUtil`, record/spec parsers, `PerforceConfig` for `.p4config` files).
- `K9.Services.Git` — `GitProvider` wrapper for `git` CLI.
- `K9.Unreal` — Unreal-specific types (`UnrealTestReport`, `UnrealTestResult`) shared between Unreal-targeting apps.

### Workspace model

`WorkspaceUtil.GetWorkspaceRoot()` walks upward from the running assembly's directory looking for **both** `Setup.bat` and `GenerateProjectFiles.bat` (the UE source-tree markers). The first directory containing both is cached as the workspace root. Apps that need workspace context (notably `K9`) fail validation if this lookup returns null.

`SettingsProvider` (constructed with that root) defines the canonical layout — matching the README's "Workspace / Folder Structure" section:

| Property | Path |
|---|---|
| `K9Folder` | `<root>/K9` |
| `SourceFolder` | `<root>/K9/Source` |
| `BinariesFolder` | `<root>/K9/Binaries` |
| `DefaultsFolder` | `<root>/K9/Defaults` (auto-created) |
| `BoostrapLibrary` | `<root>/K9/Bootstrap/K9.Workspace.Bootstrap.dll` |
| `UnrealProjectsFolder` | `<root>/Projects` (auto-created) |
| `UnrealEngineBuildBatchFilesFolder` | `<root>/Engine/Build/BatchFiles` |
| `PerforceConfigFile` | `<root>/.p4config` |

`SettingsProvider.ReplaceKeywords` expands `{ROOT}`, `{LOCAL}`, `{LOCALLOW}`, `{ROAMING}` inside strings — used by `K9` when materializing `command` / `arguments` / `workingDirectory` from `*.k9.json` before spawning.

> Note the Bootstrap caveat in `SettingsProvider.cs`: `K9.Workspace.Bootstrap` deliberately does **not** reference `K9.Core`, so if you change any of these conventions you must mirror them manually in the Bootstrap project.

### The `K9` command-synthesizer app

`Applications/K9` is a dispatcher, not a normal one-shot tool. It:

1. Enumerates `*.k9.json` files under `SettingsProvider.DefaultsFolder` and `UnrealProjectsFolder`.
2. Deserializes each into `Commands` (nested `CommandVerb` tree — `verb`/`command`/`arguments`/`workingDirectory`/`description`/`actions`).
3. Merges them into a single `CommandMap`, where the user's CLI args (e.g. `K9 ue editor build`) navigate the verb tree to resolve a leaf action.
4. Expands `{ROOT}`-style keywords in the action, injects K9-flavored env vars (`K9=1`, `Workspace`, `BatchFiles`, `K9Temp`, `COMPUTERNAME`, plus `P4CLIENT`/`P4PORT` sourced from `.p4config`), and hands off to `ProcessUtil.SpawnWithEnvironment`. Batch files are wrapped as `cmd.exe /K <bat>` so they run in a command prompt.
5. K9 exits immediately after spawning — it launches, it does not supervise.

`Applications/K9/K9.md` has a full example `.k9.json`.

### Bootstrap flow

`K9.Workspace.Bootstrap` is intentionally minimal and standalone (only depends on `Microsoft.Build.Locator`): it locates the workspace, clones/overwrites `<root>/K9/Source` from the public K9 repo, and builds everything in `Workspace` configuration so output lands in `<root>/K9/Binaries`. Then `K9.Workspace.Setup` (elevated via embedded manifest) configures env paths, P4 config, and UE prerequisites.

## Conventions

- File header is enforced by `.editorconfig` (`file_header_template`): every `.cs` file starts with `// Copyright dotBunny Inc. All Rights Reserved.` / `// See the LICENSE file at the repository root for more information.`
- C# indent is 4 spaces; `.csproj`/XML/JSON is 2 spaces.
- `dotnet_sort_system_directives_first = true` — keep `System.*` usings above others, no blank line between groups.
- Nullable reference types are enabled on every project; `ImplicitUsings` is typically **disabled** except for the top-level `K9` dispatcher.
- Each application ships a sibling `<AssemblyName>.md` file copied to output as `Content` (`PreserveNewest`) — update it when argument/flag surfaces change; `README.md` links to each.
- Common base flags supported by every app (except `K9.Workspace.Bootstrap`): `NO-PAUSE`, `QUIET`, `HELP`. `ELEVATION-CHECK` is reserved for self-elevation re-entry.