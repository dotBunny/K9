// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using System.Linq;
using K9.Core;
using K9.Core.Utils;

namespace K9.OS.FileReplacer;

internal static class Application
{
    static void Main()
    {
        using ConsoleApplication framework = new(
        new ConsoleApplicationSettings()
        {
            // ReSharper disable once StringLiteralTypo
            DefaultLogCategory = "OS.FILEREPLACER",
            LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
        });

        try
        {
            FileReplacerConfig config = FileReplacerConfig.Get(framework);
            if (config.TargetFile == null || config.SourceFile == null) return;

            string content = File.ReadAllText(config.SourceFile);
            content = config.Replaces.Aggregate(content, (current, kvp) => current.Replace(kvp.Key, kvp.Value));

            // Ensure target folder structure exists
            FileUtil.EnsureFileFolderHierarchyExists(config.TargetFile);

            File.WriteAllText(config.TargetFile, content);
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}