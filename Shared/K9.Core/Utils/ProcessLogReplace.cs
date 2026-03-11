// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace K9.Core.Utils;

public class ProcessLogReplace
{
    private readonly Action<int, string> m_Action;
    public Dictionary<string, string> Replacements = new();

    public ProcessLogReplace(ILogOutput.LogType defaultType = ILogOutput.LogType.Info, string? logCategory = null, bool replaceErrorToken = true, bool replaceWarningToken = true)
    {
        // Captures
        ILogOutput.LogType type = defaultType;
        string? category = logCategory;
        bool replaceError = replaceErrorToken;
        bool replaceWarning = replaceWarningToken;

        m_Action = (processIdentifier, line) =>
        {
            ILogOutput.LogType lineType = type;
            string processedLine = line;
            string upperLine = line.ToUpper();

            if (replaceError && upperLine.Contains("ERROR"))
            {
                lineType = ILogOutput.LogType.Error;
                processedLine = processedLine
                    .Replace("ERROR", "[[BAD]]")
                    .Replace("Error", "[[BAD]]")
                    .Replace("error", "[[BAD]]");
            }

            if (replaceError && upperLine.Contains("FAIL"))
            {
                lineType = ILogOutput.LogType.Error;

                processedLine = processedLine
                    .Replace("FAIL", "[[BAD]]")
                    .Replace("Fail", "[[BAD]]")
                    .Replace("fail", "[[BAD]]");
            }

            if (replaceWarning && upperLine.Contains("WARNING"))
            {
                lineType = ILogOutput.LogType.Warning;
                processedLine = processedLine
                    .Replace("WARNING", "[[ALERT]]")
                    .Replace("Warning", "[[ALERT]]")
                    .Replace("warning", "[[ALERT]]");
            }

            processedLine = Replacements.Aggregate(processedLine, (current, replacement) =>
                current.Replace(replacement.Key, replacement.Value));

            Log.WriteLine(processIdentifier != 0 ? $"[{processIdentifier}] {processedLine}" : processedLine, lineType, category);
        };
    }

    public Action<int, string> GetAction()
    {
        return m_Action;
    }
}