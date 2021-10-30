// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Text;
using CommandLine;
using K9.Services.Utils;
using K9.Unity.TestRunner;
using K9.Utils;
using Newtonsoft.Json;

namespace K9.Unity.Verbs
{
    [Verb("TestWrapper")]
    public class TestWrapper : IVerb
    {
        [Option('e', "executable", Required = true, HelpText = "The full path to the Unity editor to use for the tests.")]
        public string Executable { get; set; }
        [Option('d', "definition", Required = true, HelpText = "The full path to the definitions file to process.")]
        public string Definition { get; set; }
        [Option('c', "cacheServer", Required = false)]
        public string CacheServer { get; set; }
        [Option('n', "cacheServerNamespace", Required = false)]
        public string CacheServerNamespace { get; set; }
        [Option('v', "vcs", Required = false, Default = "Visible Meta Files", HelpText = "What version control mode should Unity operate in.")]
        public string VersionControlMode { get; set; }

        [Option('p', "project", Required = true)]
        public string ProjectPath { get; set; }

        private SuiteDefinition _suiteDefinition;

        private const string s_BaseArguments = "-batchmode -runTests -accept-apiupdate ";
        public bool CanExecute()
        {
            if (!System.IO.File.Exists(Executable))
            {
                Log.WriteLine($"Unable to find executable: {Executable}");
                return false;
            }

            if (string.IsNullOrEmpty(ProjectPath))
            {
                Log.WriteLine("A project is required.");
                return false;
            }
            if (System.IO.File.Exists(Definition))
            {
                _suiteDefinition = JsonConvert.DeserializeObject<SuiteDefinition>(System.IO.File.ReadAllText(Definition));
                if (_suiteDefinition != null)
                {
                    return true;
                }
                Log.WriteLine($"Unable to parse {Definition}.");
            }
            else
            {
               Log.WriteLine($"Unable to find {Definition}.");
            }
            return false;
        }

        public bool Execute()
        {

            if (_suiteDefinition.Runs.Length == 0)
            {
                Log.WriteLine("No valid runs found.");
                return true;
            }

            StringBuilder optionArgs = new();
            if (!string.IsNullOrEmpty(CacheServer))
            {
                optionArgs.AppendFormat("-EnableCacheServer -cacheServerEndpoint {0} ", CacheServer);
            }
            if (!string.IsNullOrEmpty(CacheServerNamespace))
            {
                optionArgs.AppendFormat("-cacheServerNamespacePrefix {0} ", CacheServerNamespace);
            }
            if (!string.IsNullOrEmpty(VersionControlMode))
            {
                optionArgs.AppendFormat("-vcsMode \"{0}\" ", VersionControlMode);
            }

            int runCount = _suiteDefinition.Runs.Length;
            for (int i = 0; i < runCount; i++)
            {
                RunDefinition run = _suiteDefinition.Runs[i];
                string resultsCache = System.IO.Path.GetTempFileName();
                string runArguments = $"-projectPath \"{ProjectPath}\" {s_BaseArguments}{optionArgs}{run.ToArgumentString()} -testResults {resultsCache}";

                int executionCode = Wrapper.WrapUnity(Executable, runArguments);
                if (executionCode != 0)
                {
                }
                else
                {

                }
                FileUtil.ForceDeleteFile(resultsCache);
            }
            return true;
        }


    }
}