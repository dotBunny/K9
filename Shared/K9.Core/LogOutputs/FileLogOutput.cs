// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using K9.Core.Utils;

namespace K9.Core.Loggers
{
    public class FileLogOutput : ILogOutput
    {
        readonly StreamWriter m_Writer;

        public FileLogOutput(string folder, string logName)
        {
            string path = Path.Combine(folder, $"{logName}-{DateTime.Now.ToString("yyyyMMdd_HHmmssfffffff")}.log");
            FileUtil.EnsureFileFolderHierarchyExists(path);
            m_Writer = File.CreateText(path);
        }
        ~FileLogOutput()
        {
            m_Writer.Close();
        }

        public void LineFeed()
        {
            m_Writer.WriteLineAsync(m_Writer.NewLine);
        }

        public void Shutdown()
        {
            m_Writer.Flush();
        }

        public void WriteLine(ILogOutput.LogType logType, string message)
        {
            m_Writer.WriteLineAsync($"<{ILogOutput.GetName(logType).PadRight(8, ' ')}> {message}");
        }

        public bool IsThreadSafe()
        {
            return false;
        }
    }
}