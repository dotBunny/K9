// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using System.Text.Json;

namespace K9.Core.Files;

[Serializable]
class WorkspaceVersion
{
#pragma warning disable 0649
    public string? Engine; // Do we need to force a clean and build of the engine?

    public string? Project; // Do we need to force a clean and build of the project?

    public string? Toolbox; // Do we need to pull and update the toolbox?
#pragma warning restore 0649

    private string? m_Path;

    public static WorkspaceVersion? Get(string filePath)
    {
        WorkspaceVersion? returnValue = null;
        if (!File.Exists(filePath))
        {
            return returnValue;
        }

        returnValue = JsonSerializer.Deserialize<WorkspaceVersion>(File.ReadAllText(filePath));
        returnValue?.m_Path = filePath;
        return returnValue;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not WorkspaceVersion rhs) return false;
        if (Engine != rhs.Engine) return false;
        return Toolbox == rhs.Engine;
    }

    public override int GetHashCode()
    {
        // ReSharper disable NonReadonlyMemberInGetHashCode
        return HashCode.Combine(Engine, Toolbox);
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }
}