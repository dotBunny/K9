// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using K9.Core;
using K9.Core.Utils;

namespace K9.OS.FileReplacer
{
    internal class Application
    {
        static void Main()
        {
            using ConsoleApplication framework = new(
            new K9.Core.ConsoleApplicationSettings()
            {
                DefaultLogCategory = "FILEREPLACER",
                LogOutputs = [new K9.Core.Loggers.ConsoleLogOutput()]
            });

            try
            {
                FileReplacerConfig config = FileReplacerConfig.Get(framework);
                if (config.TargetFile == null || config.SourceFile == null) return;

                string content = File.ReadAllText(config.SourceFile);
                foreach (KeyValuePair<string, string> kvp in config.Replaces)
                {
                    content = content.Replace(kvp.Key, kvp.Value);
                }

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
}