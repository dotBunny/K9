// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;

namespace K9.Services.Perforce
{
    internal class TagRecordParser : IDisposable
    {
        private readonly Action<Dictionary<string, string>> m_OutputRecord;
        private Dictionary<string, string> m_Tags = new Dictionary<string, string>();

        public TagRecordParser(Action<Dictionary<string, string>> inOutputRecord)
        {
            m_OutputRecord = inOutputRecord;
        }

        public void Dispose()
        {
            if (m_Tags.Count > 0)
            {
                m_OutputRecord(m_Tags);
                m_Tags = new Dictionary<string, string>();
            }
        }

        public void OutputLine(string line)
        {
            int spaceIndex = line.IndexOf(' ');

            string key = spaceIndex > 0 ? line[..spaceIndex] : line;
            if (m_Tags.ContainsKey(key))
            {
                m_OutputRecord(m_Tags);
                m_Tags = new Dictionary<string, string>();
            }

            m_Tags.Add(key, spaceIndex > 0 ? line[(spaceIndex + 1)..] : "");
        }
    }
}