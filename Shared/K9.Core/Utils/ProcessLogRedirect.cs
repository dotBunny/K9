// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;

namespace K9.Core.Utils;

public class ProcessLogRedirect
{
    private readonly Action<int, string> m_Action;

    public ProcessLogRedirect(ILogOutput.LogType defaultType = ILogOutput.LogType.Info, string? logCategory = null, bool parseLineForType = true)
    {
        // Captures
        ILogOutput.LogType type = defaultType;
        string? category = logCategory;
        bool shouldParseLine = parseLineForType;

        m_Action = (processIdentifier, line) =>
        {
            ILogOutput.LogType lineType = type;
            if (shouldParseLine)
            {
                string processedLine = line.ToLower();
                if (processedLine.Contains("error") || processedLine.Contains("fail"))
                {
                    lineType = ILogOutput.LogType.Error;
                }
                else if (processedLine.Contains("warning"))
                {
                    lineType = ILogOutput.LogType.Warning;
                }
            }
            Log.WriteLine(processIdentifier != 0 ? $"[{processIdentifier}] {line}" : line, lineType, category);
        };
    }

    public Action<int, string> GetAction()
    {
        return m_Action;
    }
}