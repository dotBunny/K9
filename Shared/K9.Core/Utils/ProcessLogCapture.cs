// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace K9.Core.Utils;

public class ProcessLogCapture
{
    private readonly Action<int, string> m_Action;
    private readonly List<string> m_Lines = new();

    public ProcessLogCapture()
    {
        m_Action = (_, line) => { m_Lines.Add(line); };
    }

    public void Reset()
    {
        m_Lines.Clear();
    }

    public string GetNewLinesString()
    {
        return m_Lines.Aggregate(string.Empty, (current, s) => current + s + Environment.NewLine);
    }

    public int GetLineCount()
    {
        return m_Lines.Count;
    }

    public string GetFirstLine()
    {
        return m_Lines[0];
    }

    public string GetLastLine()
    {
        return m_Lines.Last();
    }

    public bool IsFirstLineEmpty()
    {
        return m_Lines.Count == 0 || string.IsNullOrEmpty(m_Lines[0]);
    }

    public string GetString()
    {
        return m_Lines.Aggregate(string.Empty, (current, s) => current + s);
    }

    public string[] GetLines()
    {
        return m_Lines.ToArray();
    }

    public bool HasContent()
    {
        return m_Lines.Count != 0 && m_Lines.Any(line => !string.IsNullOrEmpty(line));
    }

    public Action<int, string> GetAction()
    {
        return m_Action;
    }
}