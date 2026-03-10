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

| Documentation                                                                              | Description                                                                         |
|:-------------------------------------------------------------------------------------------|:------------------------------------------------------------------------------------|
| [K9.OS.DeleteFile](Applications/K9.OS.DeleteFile/K9.OS.DeleteFile.md)                      | Deletes a file, with no nonsense.                                                   |
| [K9.OS.FileReplacer](Applications/K9.OS.FileReplacer/K9.OS.FileReplacer.md)                | A tool for replacing content in a file in one-shot.                                 |
| [K9.OS.KeepAlive](Applications/K9.OS.KeepAlive/K9.OS.KeepAlive.md)                         | An application designed to keep a launched application running, much like a service |
| [K9.OS.ScreenResolution](Applications/K9.OS.ScreenResolution/K9.OS.ScreenResolution.md)    | A tool to force a specific screen resolution.                                       |
| [K9.Publish.SteamToken](Applications/K9.Publish.SteamToken/K9.Publish.SteamToken.md)       | An application to check out and check-in the token used for SteamGuard uploads.     |
| [K9.Test.CompareImage](Applications/K9.Test.CompareImage/K9.Test.CompareImage.md)          | A tool to compare two images and fail if they are different.                        |
| [K9.Unreal.PerforceTypes](Applications/K9.Unreal.PerforceTypes/K9.Unreal.PerforceTypes.md) | A tool to detect improper types of files in Perforce for Unreal Engine source.      |
| [K9.Workspace.Bootstrap](Applications/K9.Workspace.Bootstrap/K9.Workspace.Bootstrap.md)    | A tool to bootstrap a user-workspace.                                               |
| [K9.Workspace.Reset](Applications/K9.Workspace.Reset/K9.Workspace.Reset.md)                | A tool to reset a user-workspace.                                                   |
| [K9.Workspace.Setup](Applications/K9.Workspace.Setup/K9.Workspace.Setup.md)                | A tool to setup a user-workspace.                                                   |
| [K9](Applications/K9/K9.md)                                                                | A tool to execute pre-defined tasks.                                                |

### Base Flags

While each application will have its own flags, there are some common flags shared across all applications, excluding `K9.Workspace.Bootstrap`.

| Argument   | Description                                        |
|:-----------|:---------------------------------------------------|
| `NO-PAUSE` | Do not pause and wait for any key on exit.         |
| `QUIET`    | Run in quiet mode, not asking for user input, etc. |
| `HELP`     | Show help for the application.                     |

## Workspace

### Folder Structure

| Folder          | Description                                                                                                                                |
|-----------------|--------------------------------------------------------------------------------------------------------------------------------------------|
| `/K9/Binaries`  | Where all the compiled binaries are stored when built via the `Workspace` configuration. This folder should be ignored by your chosen VCS. |
| `/K9/Bootstrap` | Where the compiled `K9.Workspace.Bootstrap` should be stored in your chosen VCS.                                                           |
| `/K9/Source`    | Where the repository should be checked out.                                                                                                |
| `/K9/Defaults`  | Where the default configuration files are stored that get read by the different applications.                                              |
