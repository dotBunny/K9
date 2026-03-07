// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace K9.Core.Utils
{
    public class ProcessLogCapture
    {
        private readonly Action<int, string> _action;
        private readonly List<string> _lines = new List<string>();

        public ProcessLogCapture()
        {
            _action = (processIdentifier, line) => { _lines.Add(line);};
        }

        public void Reset()
        {
            _lines.Clear();
        }

        public string GetNewLinesString()
        {
            return _lines.Aggregate(string.Empty, (current, s) => current + s + Environment.NewLine);
        }

        public int GetLineCount()
        {
            return _lines.Count;
        }

        public string GetFirstLine()
        {
            return _lines[0];
        }

        public bool IsFirstLineEmpty()
        {
            return _lines.Count == 0 || string.IsNullOrEmpty(_lines[0]);
        }

        public string GetString()
        {
            return _lines.Aggregate(string.Empty, (current, s) => current + s);
        }

        public string[] GetLines()
        {
            return _lines.ToArray();
        }

        public Action<int, string> GetAction()
        {
            return _action;
        }
    }
}