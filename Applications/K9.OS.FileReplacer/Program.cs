// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using System.Linq;
using K9.Core;
using K9.Core.Utils;

namespace K9.OS.FileReplacer;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
        new ConsoleApplicationSettings()
        {
            // ReSharper disable once StringLiteralTypo
            DefaultLogCategory = "FILEREPLACER",
            LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
        }, new FileReplacerProvider());

        try
        {
            FileReplacerProvider provider = (FileReplacerProvider)framework.ProgramProvider;

            if (provider.TargetFile == null || provider.SourceFile == null) return;

            string content = File.ReadAllText(provider.SourceFile);
            content = provider.Replaces.Aggregate(content, (current, kvp) => current.Replace(kvp.Key, kvp.Value));

            // Ensure target folder structure exists
            FileUtil.EnsureFileFolderHierarchyExists(provider.TargetFile);

            File.WriteAllText(provider.TargetFile, content);
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}