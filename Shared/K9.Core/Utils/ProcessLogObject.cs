// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Diagnostics;
using System.IO;

namespace K9.Core.Utils;

// ReSharper disable once InconsistentNaming
public class ProcessLogObject_StringStreamWriter(Action<string, StreamWriter> action, StreamWriter streamWriter)
{
    private readonly object m_LockObject = new();

    public void OutputHandler(object x, DataReceivedEventArgs y)
    {
        if (y.Data == null)
        {
            return;
        }

        lock (m_LockObject)
        {
            action(y.Data.TrimEnd(), streamWriter);
        }
    }
}

// ReSharper disable once InconsistentNaming
public class ProcessLogObject_IntegerString(Action<int, string> action)
{
    private int m_ProcessIdentifier;
    private readonly object m_LockObject = new();

    public void SetProcessIdentifier(int processIdentifier)
    {
        lock (m_LockObject)
        {
            m_ProcessIdentifier = processIdentifier;
        }
    }

    public void OutputHandler(object x, DataReceivedEventArgs y)
    {
        if (y.Data == null)
        {
            return;
        }

        lock (m_LockObject)
        {
            action(m_ProcessIdentifier, y.Data.TrimEnd());
        }
    }
}