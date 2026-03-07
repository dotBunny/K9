// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using static K9.Core.ILogOutput;

namespace K9.Core;

public static class Log
{
    private const string k_DateStampFormat = "yyyy-MM-dd HH:mm:ss";
    private const int k_FixedCategoryLength = 12;

    public static string DefaultCategory = "DEFAULT";

    static bool s_UseThreadSafeCache;
    static int s_LogOutputCount;
    static ILogOutput[] s_LogOutputs = [];
    static readonly System.Collections.Concurrent.ConcurrentBag<CachedLogOutput> k_ThreadSafeCache = [];

    struct CachedLogOutput(string output, LogType type)
    {
        public readonly string Output = output;
        public readonly LogType Type = type;
    }

    public static void AddLogOutput(ILogOutput output)
    {
        int newIndex = s_LogOutputCount;
        Array.Resize(ref s_LogOutputs, ++s_LogOutputCount);
        s_LogOutputs[newIndex] = output;
    }

    public static void AddLogOutputs(ILogOutput[]? outputs)
    {
        if (outputs == null) return;
        int count = outputs.Length;
        for (int i = 0; i < count; i++)
        {
            AddLogOutput(outputs[i]);
        }
    }

    public static void Shutdown()
    {
        if (!HasOutputs()) return;

        for (int i = 0; i < s_LogOutputCount; i++)
        {
            s_LogOutputs[i].Shutdown();
        }
    }

    public static void WriteLine(object output, string? category = null, LogType logType = LogType.Default)
    {
        WriteLine(output.ToString(), logType, category);
    }

    public static void WriteLine(string output, string category, LogType logType = LogType.Default)
    {
        WriteLine(output, logType, category);
    }

    public static void WriteLine(string output, LogType logType = LogType.Default, string? category = null)
    {
        if (!HasOutputs() || string.IsNullOrEmpty(output))
        {
            return;
        }

        category ??= DefaultCategory;

        if (category.Length > k_FixedCategoryLength)
        {
            category = category[..k_FixedCategoryLength];
        }

        if (s_UseThreadSafeCache)
        {
            k_ThreadSafeCache.Add(new CachedLogOutput(
                $"[{DateTime.Now.ToString(k_DateStampFormat)}] {category.ToUpper(),k_FixedCategoryLength} > {output}",
                logType));
        }

        for (int i = 0; i < s_LogOutputCount; i++)
        {
            if (s_UseThreadSafeCache && !s_LogOutputs[i].IsThreadSafe())
            {
                continue;
            }

            s_LogOutputs[i].WriteLine(logType,
                $"[{DateTime.Now.ToString(k_DateStampFormat)}] {category.ToUpper(),k_FixedCategoryLength} > {output}");
        }
    }

    public static void SetThreadSafeMode()
    {
        s_UseThreadSafeCache = true;
    }

    public static void ClearThreadSafeMode()
    {
        s_UseThreadSafeCache = false;
        CachedLogOutput[] output = k_ThreadSafeCache.ToArray();
        k_ThreadSafeCache.Clear();
        int count = output.Length;
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < s_LogOutputCount; j++)
            {
                if (s_LogOutputs[j].IsThreadSafe())
                {
                    continue;
                }

                s_LogOutputs[j].WriteLine(output[i].Type, output[i].Output);
            }
        }
    }

    public static void WriteRaw(string output, LogType logType = LogType.Default)
    {
        if (!HasOutputs()) return;

        for (int i = 0; i < s_LogOutputCount; i++)
        {
            s_LogOutputs[i].WriteLine(logType, output);
        }
    }

    public static void LineFeed()
    {
        if (!HasOutputs()) return;

        for (int i = 0; i < s_LogOutputCount; i++)
        {
            s_LogOutputs[i].LineFeed();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool HasOutputs()
    {
        return s_LogOutputCount > 0;
    }
}