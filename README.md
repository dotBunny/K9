# K9
A collection of functionality useful for automation in Game Development.

## Disclaimer
K9 is by no means the most optimized battle-ready code, nor is it meant to be. It is a finite set of functionality to augment and enhance existing automation and build systems.

## Requirements

### Git
Git needs to be accessible from your command prompt; some Git clients do not add Git to the `PATH` so it may be easier to just use the installers found at: https://git-scm.com/download/.

### .NET SDK 8.0
The bootstrapped build requires the installation of the .NET 8.0 SDK, which can be found at: https://dotnet.microsoft.com/en-us/download/dotnet/8.0

## Development

> It is important to keep your IDE building in `DEBUG` mode when actively developing as both `K9.Workspace.Bootstrap` and `K9.Workspace.Setup` have destructive actions which will wipe out any changes to the source code if ran in `RELEASE` or `WORKSPACE` mode.


## Folder Structure

| Folder          | Description                                                                                                                                |
|-----------------|--------------------------------------------------------------------------------------------------------------------------------------------|
| `/K9/Binaries`  | Where all the compiled binaries are stored when built via the `Workspace` configuration. This folder should be ignored by your chosen VCS. |
| `/K9/Bootstrap` | Where the compiled `K9.Workspace.Bootstrap` should be stored in your chosen VCS.                                                           |
| `/K9/Source`    | Where the repository should be checked out.                                                                                                |
| `/K9/Defaults`  | Where the default configuration files are stored that get read by the different applications.                                              |