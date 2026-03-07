// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;

namespace K9.Core.Utils
{
    public class ProcessLogOutput
    {
        private readonly Action<int, string> _action;
        private readonly ILogOutput.LogType _logType;

        public ProcessLogOutput(ILogOutput.LogType logType = ILogOutput.LogType.Info)
        {
            _logType = logType;
            _action = (processIdentifier, line) =>
            {
                Log.WriteLine(processIdentifier != 0 ? $"[{processIdentifier}] {line}" : line, _logType);
            };
        }

        public Action<int, string> GetAction()
        {
            return _action;
        }
    }
}