﻿// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;

namespace K9.Services.Perforce
{
    public class Spec
    {
        public List<KeyValuePair<string, string>> Sections;

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public Spec()
        {
            Sections = new List<KeyValuePair<string, string>>();
        }

        /// <summary>
        ///     Gets the current value of a field with the given name
        /// </summary>
        /// <param name="Name">Name of the field to search for</param>
        /// <returns>The value of the field, or null if it does not exist</returns>
        public string GetField(string Name)
        {
            int Idx = Sections.FindIndex(x => x.Key == Name);
            return Idx == -1 ? null : Sections[Idx].Value;
        }

        /// <summary>
        ///     Sets the value of an existing field, or adds a new one with the given name
        /// </summary>
        /// <param name="Name">Name of the field to set</param>
        /// <param name="Value">New value of the field</param>
        public void SetField(string Name, string Value)
        {
            int Idx = Sections.FindIndex(x => x.Key == Name);
            if (Idx == -1)
            {
                Sections.Add(new KeyValuePair<string, string>(Name, Value));
            }
            else
            {
                Sections[Idx] = new KeyValuePair<string, string>(Name, Value);
            }
        }

        /// <summary>
        ///     Parses a spec (clientspec, branchspec, changespec) from an array of lines
        /// </summary>
        /// <param name="Lines">Text split into separate lines</param>
        /// <returns>Array of section names and values</returns>
        public static bool TryParse(List<string> Lines, out Spec Spec)
        {
            Spec = new Spec();
            for (int LineIdx = 0; LineIdx < Lines.Count; LineIdx++)
            {
                if (Lines[LineIdx].EndsWith("\r"))
                {
                    Lines[LineIdx] = Lines[LineIdx].Substring(0, Lines[LineIdx].Length - 1);
                }

                if (!string.IsNullOrWhiteSpace(Lines[LineIdx]) && !Lines[LineIdx].StartsWith("#"))
                {
                    // Read the section name
                    int SeparatorIdx = Lines[LineIdx].IndexOf(':');
                    if (SeparatorIdx == -1 || !char.IsLetter(Lines[LineIdx][0]))
                    {
                        Log.WriteLine($"Invalid spec format at line {LineIdx}: \"{Lines[LineIdx]}\"", "P4");
                        return false;
                    }

                    // Get the section name
                    string SectionName = Lines[LineIdx].Substring(0, SeparatorIdx);

                    // Parse the section value
                    StringBuilder Value = new(Lines[LineIdx].Substring(SeparatorIdx + 1).TrimStart());
                    for (; LineIdx + 1 < Lines.Count; LineIdx++)
                    {
                        if (Lines[LineIdx + 1].Length == 0)
                        {
                            Value.AppendLine();
                        }
                        else if (Lines[LineIdx + 1][0] == '\t')
                        {
                            Value.AppendLine(Lines[LineIdx + 1].Substring(1));
                        }
                        else
                        {
                            break;
                        }
                    }

                    Spec.Sections.Add(new KeyValuePair<string, string>(SectionName, Value.ToString().TrimEnd()));
                }
            }

            return true;
        }

        /// <summary>
        ///     Formats a P4 specification as a block of text
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder Result = new();
            foreach (KeyValuePair<string, string> Section in Sections)
            {
                if (Section.Value.Contains('\n'))
                {
                    Result.AppendLine(Section.Key + ":\n\t" + Section.Value.Replace("\n", "\n\t"));
                }
                else
                {
                    Result.AppendLine(Section.Key + ":\t" + Section.Value);
                }

                Result.AppendLine();
            }

            return Result.ToString();
        }
    }
}