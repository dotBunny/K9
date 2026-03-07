// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using System.Text;

namespace K9.Services.Perforce;

public class Spec
{
    readonly List<KeyValuePair<string, string>> m_Sections;

    /// <summary>
    ///     Default constructor.
    /// </summary>
    Spec()
    {
        m_Sections = [];
    }

    /// <summary>
    ///     Gets the current value of a field with the given name
    /// </summary>
    /// <param name="name">Name of the field to search for</param>
    /// <returns>The value of the field, or null if it does not exist</returns>
    public string? GetField(string name)
    {
        int index = m_Sections.FindIndex(x => x.Key == name);
        return index == -1 ? null : m_Sections[index].Value;
    }

    /// <summary>
    ///     Sets the value of an existing field or adds a new one with the given name
    /// </summary>
    /// <param name="name">Name of the field to set</param>
    /// <param name="value">New value of the field</param>
    public void SetField(string name, string value)
    {
        int index = m_Sections.FindIndex(x => x.Key == name);
        if (index == -1)
        {
            m_Sections.Add(new KeyValuePair<string, string>(name, value));
        }
        else
        {
            m_Sections[index] = new KeyValuePair<string, string>(name, value);
        }
    }

    // ReSharper disable once CommentTypo
    /// <summary>
    ///     Parses a spec (clientspec, branchspec, changespec) from an array of lines
    /// </summary>
    /// <param name="lines">Text split into separate lines</param>
    /// <param name="spec">The parsed Spec</param>
    /// <returns>Array of section names and values</returns>
    public static bool TryParse(List<string> lines, out Spec spec)
    {
        spec = new Spec();
        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            if (lines[lineIndex].EndsWith("\r"))
            {
                lines[lineIndex] = lines[lineIndex][..^1];
            }

            if (string.IsNullOrWhiteSpace(lines[lineIndex]) || lines[lineIndex].StartsWith("#"))
            {
                continue;
            }

            // Read the section name
            int sepIndex = lines[lineIndex].IndexOf(':');
            if (sepIndex == -1 || !char.IsLetter(lines[lineIndex][0]))
            {
                Core.Log.WriteLine($"Invalid spec format at line {lineIndex}: \"{lines[lineIndex]}\"", PerforceProvider.LogCategory);
                return false;
            }

            // Get the section name
            string sectionName = lines[lineIndex][..sepIndex];

            // Parse the section value
            StringBuilder value = new(lines[lineIndex][(sepIndex + 1)..].TrimStart());
            for (; lineIndex + 1 < lines.Count; lineIndex++)
            {
                if (lines[lineIndex + 1].Length == 0)
                {
                    value.AppendLine();
                }
                else if (lines[lineIndex + 1][0] == '\t')
                {
                    value.AppendLine(lines[lineIndex + 1][1..]);
                }
                else
                {
                    break;
                }
            }

            spec.m_Sections.Add(new KeyValuePair<string, string>(sectionName, value.ToString().TrimEnd()));
        }

        return true;
    }

    /// <summary>
    ///     Formats a P4 specification as a block of text
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        StringBuilder result = new();
        foreach (KeyValuePair<string, string> section in m_Sections)
        {
            if (section.Value.Contains('\n'))
            {
                result.AppendLine(section.Key + ":\n\t" + section.Value.Replace("\n", "\n\t"));
            }
            else
            {
                result.AppendLine(section.Key + ":\t" + section.Value);
            }

            result.AppendLine();
        }

        return result.ToString();
    }
}