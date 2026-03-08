# K9

A collection of functionality useful for automation in Game Development.

> **Disclaimer**<br /> K9 is by no means the most optimized battle-ready code, nor is it meant to be. It is a finite set of functionality built for purpose over the years to augment and enhance existing automation and build systems.

## Requirements

### Git

Git needs to be accessible from your command prompt; some Git clients do not add Git to the `PATH` so it may be easier to just use the installers found at: https://git-scm.com/download/.

### .NET SDK 10.0

The bootstrapped build requires the installation of the .NET 10.0 SDK, which can be found at: https://dotnet.microsoft.com/en-us/download/dotnet/10.0.
In 2026, a modernization effort was made to bring all applications to target C# 14 w/ .NET Runtime 10, and libraries targeting C# 14 w/ .NET Standard 2.1.

## Development

> It is important to keep your IDE building in `DEBUG` mode when actively developing as both `K9.Workspace.Bootstrap` and `K9.Workspace.Setup` have destructive actions which will wipe out any changes to the source code if ran in `RELEASE` or `WORKSPACE` mode.

## Applications

| Documentation                                                                              | Description                                                                    |
|:-------------------------------------------------------------------------------------------|:-------------------------------------------------------------------------------|
| [K9.OS.FileReplacer](Applications/K9.OS.FileReplacer/K9.OS.FileReplacer.md)                | A tool to replace content in an existing file.                                 |
| [K9.OS.KeepAlive](Applications/K9.OS.KeepAlive/K9.OS.KeepAlive.md)                         | A tool to monitor and restart a given process.                                 |
| [K9.OS.ScreenResolution](Applications/K9.OS.ScreenResolution/K9.OS.ScreenResolution.md)    | A tool to force a specific screen resolution.                                  |
| [K9.Publish.SteamToken](Applications/K9.Publish.SteamToken/K9.Publish.SteamToken.md)       | A tool to support SteamGuard token storage and retrieval.                      |
| [K9.Unreal.PerforceTypes](Applications/K9.Unreal.PerforceTypes/K9.Unreal.PerforceTypes.md) | A tool to detect improper types of files in Perforce for Unreal Engine source. |
| [K9.Workspace.Bootstrap](Applications/K9.Workspace.Bootstrap/K9.Workspace.Bootstrap.md)    | A tool to bootstrap a user-workspace.                                          |
| [K9.Workspace.Reset](Applications/K9.Workspace.Reset/K9.Workspace.Reset.md)                | A tool to reset a user-workspace.                                              |
| [K9.Workspace.Setup](Applications/K9.Workspace.Setup/K9.Workspace.Setup.md)                | A tool to setup a user-workspace.                                              |
| [K9](Applications/K9/K9.md)                                                                | A tool to execute pre-defined tasks.                                           |

## Workspace

### Folder Structure

| Folder          | Description                                                                                                                                |
|-----------------|--------------------------------------------------------------------------------------------------------------------------------------------|
| `/K9/Binaries`  | Where all the compiled binaries are stored when built via the `Workspace` configuration. This folder should be ignored by your chosen VCS. |
| `/K9/Bootstrap` | Where the compiled `K9.Workspace.Bootstrap` should be stored in your chosen VCS.                                                           |
| `/K9/Source`    | Where the repository should be checked out.                                                                                                |
| `/K9/Defaults`  | Where the default configuration files are stored that get read by the different applications.                                              |
