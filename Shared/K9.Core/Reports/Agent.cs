// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using K9.Core.Extensions;

namespace K9.Core.Reports;

public class Agent
{
    private readonly bool m_UsedDefaultValues;

    // ReSharper disable MemberCanBePrivate.Global
    // ReSharper disable InconsistentNaming
    public readonly string CPU;
    public readonly string GPU;
    public readonly string Memory;
    public readonly string Name;
    // ReSharper restore InconsistentNaming
    // ReSharper restore MemberCanBePrivate.Global

    public Agent(string name)
    {
        Name = name;

        CPU = string.Empty;
        GPU = string.Empty;
        Memory = string.Empty;
    }

    public Agent(string inputData, Agent? defaultAgent = null)
    {
        Name = inputData.MarkedSubstring("DeviceName\":\"", "\"");
        CPU = inputData.MarkedSubstring("ProcessorType\":\"", "\"");
        GPU = inputData.MarkedSubstring("GraphicsDeviceName\":\"", "\"");
        Memory = inputData.MarkedSubstring("SystemMemorySizeMB\":", ",").TrimEnd('}');

        if (defaultAgent == null)
        {
            return;
        }

        if (Name.Length == 0)
        {
            Name = defaultAgent.Name;
            m_UsedDefaultValues = true;
        }

        if (CPU.Length == 0)
        {
            CPU = defaultAgent.CPU;
            m_UsedDefaultValues = true;
        }

        if (GPU.Length == 0)
        {
            GPU = defaultAgent.GPU;
            m_UsedDefaultValues = true;
        }

        if (Memory.Length == 0)
        {
            Memory = defaultAgent.Memory;
            m_UsedDefaultValues = true;
        }
    }

    public bool IsValid()
    {
        return Name.Length > 0 && !m_UsedDefaultValues;
    }

    public object[] GetObjectArray()
    {
        return [Name, CPU, GPU, Memory];
    }
}