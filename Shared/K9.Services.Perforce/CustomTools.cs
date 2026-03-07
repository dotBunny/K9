// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Collections.Generic;
using K9.Core.Utils;
using K9.Core;

namespace K9.Services.Perforce;

public static class CustomTools
{
    // ReSharper disable once StringLiteralTypo
    public static readonly string ConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".p4qt", "customtools.xml");

    public static CustomToolDefList Get(string? customPath = null)
    {
        customPath ??= ConfigFile;

        if (!File.Exists(customPath))
        {
            Log.WriteLine($"Unable to find provided tool file @ {customPath}, returning empty data.", "PERFORCE", ILogOutput.LogType.Warning);
            return new CustomToolDefList();
        }

        XmlSerializer serializer = new(typeof(CustomToolDefList));
        using StreamReader reader = new(customPath);
        CustomToolDefList? returnObject;
        try
        {
            returnObject = (CustomToolDefList)serializer.Deserialize(reader);
        }
        catch
        {
            Log.WriteLine($"Unable to parse provided tool file @ {customPath}, returning empty data.", "PERFORCE", ILogOutput.LogType.Warning);
            returnObject = new CustomToolDefList();
        }
        return returnObject;
    }


    public static void AddOrReplace(this CustomToolDefList lhs, CustomToolDefList rhs)
    {
        // Handle Folders
        int rhsFolderCount = rhs.CustomToolFolders.Count;
        for (int j = 0; j < rhsFolderCount; j++)
        {
            int lhsFolderCount = lhs.CustomToolFolders.Count;
            bool found = false;
            for (int i = 0; i < lhsFolderCount; i++)
            {
                if (lhs.CustomToolFolders[i].Name == rhs.CustomToolFolders[j].Name)
                {
                    lhs.CustomToolFolders[i] = rhs.CustomToolFolders[j];
                    found = true;
                }
            }
            if (!found)
            {
                lhs.CustomToolFolders.Add(rhs.CustomToolFolders[j]);
            }
        }

        // Handle Tools
        int rhsToolCount = rhs.CustomToolDefs.Count;
        for (int j = 0; j < rhsToolCount; j++)
        {
            int lsToolCount = lhs.CustomToolDefs.Count;
            bool found = false;
            for (int i = 0; i < lsToolCount; i++)
            {

                if (lhs.CustomToolDefs[i]?.Definition?.Name == rhs.CustomToolDefs[j]?.Definition?.Name)
                {
                    lhs.CustomToolDefs[i] = rhs.CustomToolDefs[j];
                    found = true;
                }
            }
            if (!found)
            {
                lhs.CustomToolDefs.Add(rhs.CustomToolDefs[j]);
            }
        }
    }

    public static void Output(this CustomToolDefList definition, string path)
    {
        FileUtil.EnsureFileFolderHierarchyExists(path);

        XmlWriterSettings settings = new() { Indent = true };
        XmlSerializer serializer = new(typeof(CustomToolDefList));
        using XmlWriter writer = XmlWriter.Create(path, settings);
        serializer.Serialize(writer, definition);
    }

    #region File DOM Definition
    [XmlRoot(ElementName = "Definition")]
    public class Definition
    {
        [XmlElement(ElementName = "Name")]
        public string? Name { get; set; }

        [XmlElement(ElementName = "Command")]
        public string? Command { get; set; }

        [XmlElement(ElementName = "Arguments")]
        public string? Arguments { get; set; }

        [XmlElement(ElementName = "Shortcut")]
        public object? Shortcut { get; set; }
    }

    [XmlRoot(ElementName = "Console")]
    public class Console
    {
        [XmlElement(ElementName = "CloseOnExit")]
        public bool CloseOnExit { get; set; }
    }

    [XmlRoot(ElementName = "CustomToolDef")]
    public class CustomToolDef
    {
        [XmlElement(ElementName = "Definition")]
        public Definition? Definition { get; set; }

        [XmlElement(ElementName = "Console")]
        public Console? Console { get; set; }

        [XmlElement(ElementName = "AddToContext")]
        public bool AddToContext { get; set; }
        [XmlElement(ElementName = "Refresh")]
        public bool Refresh { get; set; }
    }

    [XmlRoot(ElementName = "CustomToolDefList")]
    public class CustomToolDefList
    {
        [XmlElement(ElementName = "CustomToolDef")]
        public List<CustomToolDef> CustomToolDefs { get; set; } = [];

        [XmlElement(ElementName = "CustomToolFolder")]
        public List<CustomToolFolder> CustomToolFolders { get; set; } = [];

        [XmlAttribute(AttributeName = "varName")]
        // ReSharper disable once StringLiteralTypo
        public string VarName { get; set; } = "customtooldeflist";

        [XmlText]
        public string? Text { get; set; }
    }

    [XmlRoot(ElementName = "CustomToolFolder")]
    public class CustomToolFolder
    {
        [XmlElement(ElementName = "Name")]
        public string? Name { get; set; }

        [XmlElement(ElementName = "CustomToolDefList")]
        public CustomToolDefList? CustomToolDefList { get; set; }
    }
    #endregion
}
