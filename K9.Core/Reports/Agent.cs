using K9.Utils;

namespace K9.Reports
{
    public class Agent
    {
        public readonly string Name;
        public readonly string CPU;
        public readonly string GPU;
        public readonly string Memory;
        

        private readonly bool _usedDefaultValues;

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

            if (defaultAgent == null) return;

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
            return new object[] {Name, CPU, GPU, Memory};
        }
    }
}