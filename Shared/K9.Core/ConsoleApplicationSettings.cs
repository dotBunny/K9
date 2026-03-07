// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

namespace K9.Core;

public class ConsoleApplicationSettings
{
    public string? DefaultLogCategory;
    public ILogOutput[]? LogOutputs;
    public bool PauseOnExit = false;
    public bool RequiresElevatedAccess = false;
    public bool DisplayHeader = true;
    public bool DisplayRuntime = true;
}