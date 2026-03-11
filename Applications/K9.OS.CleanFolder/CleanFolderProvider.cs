// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using K9.Core;
using K9.Core.Modules;

namespace K9.OS.CleanFolder;

public class CleanFolderProvider : ProgramProvider
{
    public string? TargetFolder;

    string[] m_ExcludeFilePrefix = [];
    string[] m_ExcludeFileSuffix = [];
    string[] m_ExcludeFile = [];

    string[] m_ExcludePathPrefix = [];
    string[] m_ExcludePathSuffix = [];
    string[] m_ExcludePath = [];

    string[] m_ExcludeFolder = [];

    bool m_ShouldIgnoreCase = true;
    public bool ShouldDeleteEmptyDirectories = true;

    public override string GetDescription()
    {
        return "Set different environment variables based on inputs.";
    }

    public override KeyValuePair<string, string>[] GetArgumentHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[8];

        lines[0] = new KeyValuePair<string, string>("TARGET", "The absolute path of the folder to clean.");

        lines[1] = new KeyValuePair<string, string>("EXCLUDE-FILE-PREFIX", "A comma delimited list of file name prefixes  to exclude from the clean.");
        lines[2] = new KeyValuePair<string, string>("EXCLUDE-FILE-SUFFIX", "A comma delimited list of file name suffixes to exclude from the clean.");
        lines[3] = new KeyValuePair<string, string>("EXCLUDE-FILE", "A comma delimited list of file names to exclude from the clean.");
        lines[4] = new KeyValuePair<string, string>("EXCLUDE-FOLDER", "A comma delimited list of folder names to exclude from the clean.");
        lines[5] = new KeyValuePair<string, string>("EXCLUDE-PATH-PREFIX", "A comma delimited list of path prefixes to exclude from the clean.");
        lines[6] = new KeyValuePair<string, string>("EXCLUDE-PATH-SUFFIX", "A comma delimited list of path suffixes to exclude from the clean.");
        lines[7] = new KeyValuePair<string, string>("EXCLUDE-PATH", "A comma delimited list of paths to exclude from the clean.");

        return lines;
    }

    public override KeyValuePair<string, string>[] GetFlagHelp()
    {
        KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[2];

        lines[0] = new KeyValuePair<string, string>("CHECK-CASE", "Does the case of the file names and paths matter?");
        lines[1] = new KeyValuePair<string, string>("IGNORE-EMPTY-DIR", "Should empty directories not be deleted?");

        return lines;
    }

    public override bool IsValid(ArgumentsModule args)
    {
        if (args.HasOverrideArgument("TARGET"))
        {
            if (!Directory.Exists(args.GetOverrideArgument("TARGET")))
            {
                Log.WriteLine($"Unable to find TARGET @ {args.GetOverrideArgument("TARGET")}", ILogOutput.LogType.Warning);
                return false;
            }
        }
        else
        {
            Log.WriteLine("A TARGET folder is required (---TARGET=/my/folder)");
            return false;
        }

        return base.IsValid(args);
    }

    public override void ParseArguments(ArgumentsModule args)
    {
        TargetFolder = args.GetOverrideArgument("TARGET");
        m_ShouldIgnoreCase = !args.HasBaseArgument("CHECK-CASE");

        // File Excludes
        if (args.HasOverrideArgument("EXCLUDE-FILE-PREFIX"))
        {
            m_ExcludeFilePrefix = args.GetOverrideArgument("EXCLUDE-FILE-PREFIX")
                .Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (m_ShouldIgnoreCase)
            {
                for (int i = 0; i < m_ExcludeFilePrefix.Length; i++)
                {
                    m_ExcludeFilePrefix[i] = m_ExcludeFilePrefix[i].ToLower();
                }
            }
        }
        if (args.HasOverrideArgument("EXCLUDE-FILE-SUFFIX"))
        {
            m_ExcludeFileSuffix = args.GetOverrideArgument("EXCLUDE-FILE-SUFFIX")
                .Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (m_ShouldIgnoreCase)
            {
                for (int i = 0; i < m_ExcludeFileSuffix.Length; i++)
                {
                    m_ExcludeFileSuffix[i] = m_ExcludeFileSuffix[i].ToLower();
                }
            }
        }
        if (args.HasOverrideArgument("EXCLUDE-FILE"))
        {
            m_ExcludeFile = args.GetOverrideArgument("EXCLUDE-FILE")
                .Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (m_ShouldIgnoreCase)
            {
                for (int i = 0; i < m_ExcludeFile.Length; i++)
                {
                    m_ExcludeFile[i] = m_ExcludeFile[i].ToLower();
                }
            }
        }

        if (args.HasOverrideArgument("EXCLUDE-FOLDER"))
        {
            m_ExcludeFolder = args.GetOverrideArgument("EXCLUDE-FOLDER")
                .Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (m_ShouldIgnoreCase)
            {
                for (int i = 0; i < m_ExcludeFolder.Length; i++)
                {
                    m_ExcludeFolder[i] = m_ExcludeFolder[i].ToLower();
                }
            }
        }

        if (args.HasOverrideArgument("EXCLUDE-PATH-PREFIX"))
        {
            m_ExcludePathPrefix = args.GetOverrideArgument("EXCLUDE-PATH-PREFIX")
                .Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (m_ShouldIgnoreCase)
            {
                for (int i = 0; i < m_ExcludePathPrefix.Length; i++)
                {
                    m_ExcludePathPrefix[i] = m_ExcludePathPrefix[i].ToLower();
                }
            }
        }

        if (args.HasOverrideArgument("EXCLUDE-PATH-SUFFIX"))
        {
            m_ExcludePathSuffix = args.GetOverrideArgument("EXCLUDE-PATH-SUFFIX")
                .Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (m_ShouldIgnoreCase)
            {
                for (int i = 0; i < m_ExcludePathSuffix.Length; i++)
                {
                    m_ExcludePathSuffix[i] = m_ExcludePathSuffix[i].ToLower();
                }
            }
        }

        if (args.HasOverrideArgument("EXCLUDE-PATH"))
        {
            m_ExcludePath = args.GetOverrideArgument("EXCLUDE-PATH")
                .Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (m_ShouldIgnoreCase)
            {
                for (int i = 0; i < m_ExcludePath.Length; i++)
                {
                    m_ExcludePath[i] = m_ExcludePath[i].ToLower();
                }
            }
        }

        ShouldDeleteEmptyDirectories = !args.HasBaseArgument("IGNORE-EMPTY-DIR");



        base.ParseArguments(args);
    }

    public bool IsExcludedFileName(string? fileName)
    {
        if (fileName == null) return false;
        string query = fileName;
        if(m_ShouldIgnoreCase)
        {
            query = query.ToLower();
        }
        return m_ExcludeFilePrefix.Any(query.StartsWith) ||
               m_ExcludeFileSuffix.Any(query.EndsWith) ||
               m_ExcludeFile.Any(s => query == s);
    }

    public bool IsExcludedPath(string? absolutePath)
    {
        if (absolutePath == null) return false;
        string query = absolutePath;
        if(m_ShouldIgnoreCase)
        {
            query = query.ToLower();
        }
        return m_ExcludePathPrefix.Any(query.StartsWith) ||
               m_ExcludePathSuffix.Any(query.EndsWith) ||
               m_ExcludePath.Any(s => query == s);
    }

    public bool IsExcludedFolder(string? absolutePath)
    {
        if (absolutePath == null) return false;
        string query = absolutePath;
        if(m_ShouldIgnoreCase)
        {
            query = query.ToLower();
        }
        string[] folders = query.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        return folders.Any(folder => m_ExcludeFolder.Any(name => folder == name));
    }
}