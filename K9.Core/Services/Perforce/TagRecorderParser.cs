using System;
using System.Collections.Generic;

namespace K9.Services.Perforce
{
    internal class TagRecordParser : IDisposable
    {
        private readonly Action<Dictionary<string, string>> OutputRecord;
        private Dictionary<string, string> Tags = new();

        public TagRecordParser(Action<Dictionary<string, string>> InOutputRecord)
        {
            OutputRecord = InOutputRecord;
        }

        public void Dispose()
        {
            if (Tags.Count > 0)
            {
                OutputRecord(Tags);
                Tags = new Dictionary<string, string>();
            }
        }

        public void OutputLine(string Line)
        {
            int SpaceIdx = Line.IndexOf(' ');

            string Key = SpaceIdx > 0 ? Line.Substring(0, SpaceIdx) : Line;
            if (Tags.ContainsKey(Key))
            {
                OutputRecord(Tags);
                Tags = new Dictionary<string, string>();
            }

            Tags.Add(Key, SpaceIdx > 0 ? Line.Substring(SpaceIdx + 1) : "");
        }
    }
}