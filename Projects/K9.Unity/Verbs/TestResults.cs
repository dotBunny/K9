// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using CommandLine;
using K9.Reports;
using K9.Services.Google;
using K9.Services.Office;
using K9.Unity.TestRunner;
using K9.Unity.TestRunner.Report;
using K9.Utils;

namespace K9.Unity.Verbs
{
    [Verb("TestResults")]
    public class TestResults : DefaultOptions, IVerb
    {
        private const string SearchPattern = "unityTestResults-*.xml";
        private const string ConsoleCategory = "TESTRESULTS";


        [Option('i', "input", Required = false, HelpText = "Unity tests runner results file to process.")]
        public string Input { get; set; }

        [Option('o', "output", Required = false, HelpText = "Where to output processed report of tests.")]
        public string Output { get; set; }

        [Option('c', "copy", Required = false, HelpText = "Do you want a copy of the input file to be made? To Where?")]
        public string Copy { get; set; }

        [Option('g', "google", Required = false,
            HelpText = "Send generated data from tests, if appropriate to Google API.")]
        public bool OutputToGoogle { get; set; }

        [Option('x', "xls", Required = false, HelpText = "Update excel documents with generated data.")]
        public bool OutputToExcel { get; set; }

        [Option('r', "reports", Required = false, HelpText = "Report path override")]
        public string ReportPath { get; set; }

        [Option('v', "changelist", Required = false, HelpText = "The changelist the tests were run at.")]
        public string Changelist { get; set; }

        [Option('p', "platform", Required = false, HelpText = "Platform of reports being analyzed.")]
        public string Platform { get; set; }

        public bool CanExecute()
        {
            if (!string.IsNullOrEmpty(GetTargetInput()))
            {
                return true;
            }

            Log.WriteLine("Unable to find input/folder.");
            return false;
        }

        public bool Execute()
        {
            Log.WriteLine("Executing ...", ConsoleCategory);

            ProcessDefaultOptions();


            string fullPath = GetTargetInput();

            // Should we be copying?
            if (!string.IsNullOrEmpty(Copy))
            {
                string outputPath = Path.GetFullPath(Copy.FixDirectorySeparator());
                outputPath.MakeWritable();

                Log.WriteLine($"Copying {fullPath} to {outputPath}", ConsoleCategory);
                File.Copy(fullPath, outputPath, true);
            }

            Log.WriteLine($"Reading {fullPath}", ConsoleCategory);
            XmlSerializer xml = new(typeof(TestRun), new XmlRootAttribute("test-run"));

            MemoryStream stream = fullPath.GetMemoryStream();
            stream.Seek(0, SeekOrigin.Begin);

            TestRun testRun = (TestRun)xml.Deserialize(stream);
            if (testRun != null)
            {
                List<IResult> results = testRun.GetTestCases().GetResults();


                // Output Report
                StringBuilder report = new();
                foreach (IResult r in results)
                {
                    report.AppendLine(r.ToString());
                }

                if (!string.IsNullOrEmpty(Output))
                {
                    string outputPath = Path.GetFullPath(Output.FixDirectorySeparator());
                    outputPath.MakeWritable();
                    File.WriteAllText(outputPath, report.ToString());
                }

                // Output Report To Log
                Log.LineFeed();
                Log.WriteRaw(report.ToString());


                if (OutputToGoogle)
                {
                    if (SheetsUtil.Post(
                        Path.GetFullPath(Path.Combine(Core.WorkspaceRoot,
                            Core.Settings.Data["GoogleAPI"]["KeyPath"].FixDirectorySeparator())),
                        Core.Settings.Data["GoogleAPI"]["ApplicationName"], ref results))
                    {
                        Log.WriteLine("Upload Complete.", "GOOGLE");
                    }
                    else
                    {
                        Log.WriteLine("FAILED to upload results.", "GOOGLE");
                    }
                }

                if (OutputToExcel)
                {
                    string reportPath = Path.Combine(Core.WorkspaceRoot, "Reports");

                    if (Core.Settings.Data["Reports"] != null &&
                        Core.Settings.Data["Reports"]["OutputFolder"] != null &&
                        Directory.Exists(Path.GetFullPath(Core.Settings.Data["Reports"]["OutputFolder"])))
                    {
                        reportPath = Path.GetFullPath(Core.Settings.Data["Reports"]["OutputFolder"]);
                    }

                    if (!string.IsNullOrEmpty(ReportPath) && Directory.Exists(Path.GetFullPath(ReportPath)))
                    {
                        reportPath = Path.GetFullPath(ReportPath);
                    }

                    ExcelUtil.Post(reportPath, ref results);
                }
            }

            return true;
        }

        internal void ProcessDefaultOptions()
        {
            if (!string.IsNullOrEmpty(Changelist) && !Core.OverrideArguments.ContainsKey(Core.ChangelistKey))
            {
                Core.Changelist = Changelist;
            }

            if (!string.IsNullOrEmpty(Platform) && !Core.OverrideArguments.ContainsKey(Core.PlatformKey))
            {
                Core.Platform = Platform;
            }
        }


        public string GetTargetInput()
        {
            if (!string.IsNullOrEmpty(Input))
            {
                string fullPath = Path.GetFullPath(Input.FixDirectorySeparator());
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            if (!string.IsNullOrEmpty(Folder))
            {
                string fullPath = Path.GetFullPath(Folder);
                if (Directory.Exists(fullPath))
                {
                    string[] files = Directory.GetFiles(fullPath, SearchPattern, SearchOption.TopDirectoryOnly);
                    if (files.Length > 0)
                    {
                        return files[0];
                    }
                }
            }

            return null;
        }
    }
}