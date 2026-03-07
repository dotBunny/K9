// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;

namespace K9.Core.Utils;

public class ProcessLogRedirect
{
    private readonly Action<int, string> m_Action;

    public ProcessLogRedirect(ILogOutput.LogType logType = ILogOutput.LogType.Info)
    {
        ILogOutput.LogType type = logType;
        m_Action = (processIdentifier, line) =>
        {
            Log.WriteLine(processIdentifier != 0 ? $"[{processIdentifier}] {line}" : line, type);
        };
    }

    public Action<int, string> GetAction()
    {
        return m_Action;
    }
}