// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using K9.Utils;

namespace K9.Reports
{
    public class Agent
    {
        private readonly bool _usedDefaultValues;
        public readonly string CPU;
        public readonly string GPU;
        public readonly string Memory;
        public readonly string Name;

        public Agent(string name)
        {
            Name = name;
        }

        public Agent(string inputData, Agent defaultAgent = null)
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
                _usedDefaultValues = true;
            }

            if (CPU.Length == 0)
            {
                CPU = defaultAgent.CPU;
                _usedDefaultValues = true;
            }

            if (GPU.Length == 0)
            {
                GPU = defaultAgent.GPU;
                _usedDefaultValues = true;
            }

            if (Memory.Length == 0)
            {
                Memory = defaultAgent.Memory;
                _usedDefaultValues = true;
            }
        }

        public bool IsValid()
        {
            return Name.Length > 0 && !_usedDefaultValues;
        }

        public object[] GetObjectArray()
        {
            return new object[] { Name, CPU, GPU, Memory };
        }
    }
}