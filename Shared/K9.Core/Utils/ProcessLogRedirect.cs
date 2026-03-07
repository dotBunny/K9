// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;

namespace K9.Core.Utils;

public class ProcessLogRedirect
{
    private readonly Action<int, string> m_Action;

    public ProcessLogRedirect(ILogOutput.LogType logType = ILogOutput.LogType.Info, string? logCategory = null)
    {
        ILogOutput.LogType type = logType;
        string? category = logCategory;
        m_Action = (processIdentifier, line) =>
        {
            Log.WriteLine(processIdentifier != 0 ? $"[{processIdentifier}] {line}" : line, type, category);
        };
    }

    public Action<int, string> GetAction()
    {
        return m_Action;
    }
}