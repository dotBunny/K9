using System.Collections.Generic;

namespace K9.Services.Perforce
{
    public class StreamRecord
    {
        public string Identifier;
        public string Name;
        public string Parent;

        public StreamRecord(Dictionary<string, string> Tags)
        {
            Tags.TryGetValue("Stream", out Identifier);
            Tags.TryGetValue("Name", out Name);
            if (Tags.TryGetValue("Parent", out Parent) && Parent == "none")
            {
                Parent = null;
            }
        }
    }
}