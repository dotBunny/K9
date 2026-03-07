// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

namespace K9.Core;

public interface ILogOutput
{
    public enum LogType
    {
        Default,
        Notice,
        Info,
        ExternalProcess,
        Warning,
        Error
    }

    public static string GetName(LogType type)
    {
        return type switch
        {
            LogType.Notice => "NOTICE",
            LogType.Info => "INFO",
            LogType.ExternalProcess => "EXTERNAL",
            LogType.Warning => "WARNING",
            LogType.Error => "ERROR",
            _ => "DEFAULT"
        };
    }

    public void WriteLine(LogType logType, string message);
    public void LineFeed();

    public void Shutdown();

    public bool IsThreadSafe();
}
