using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using CommandLine;
using Newtonsoft.Json.Linq;

namespace K9.Unreal.Verbs
{
    [Verb("UERunTestsJSONToNUnitXML")]
    public class UERunTestsJSONToNUnitXML : DefaultOptions, IVerb
    {
        [Option('i', "inputPath", Required = false, HelpText = "The path to the source file to be converted.")]
        public string InputPath { get; set; }

        [Option('o', "outputPath", Required = true, HelpText = "The path to output the converted file.")]
        public string OutputPath { get; set; }

        [Option('s', "suiteName", Required = false, HelpText = "Set the test suite name.", Default = "UAT")]
        public string Suite { get; set; }

        public bool CanExecute()
        {
            if (string.IsNullOrEmpty(InputPath) || !File.Exists(InputPath)) return false;

            if (string.IsNullOrEmpty(OutputPath)) return false;

            // Best Guess JSON
            try
            {
                var jsonObject = JObject.Parse(File.ReadAllText(InputPath));
                if (jsonObject == null) return false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool Execute()
        {
            var source = JObject.Parse(File.ReadAllText(InputPath));

            // Create Header
            var doc = new XmlDocument();
            var xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", "no");
            var root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            // Create Results Root
            var rootResults = doc.CreateElement(string.Empty, "test-results", string.Empty);
            rootResults.SetAttribute("name", source["clientDescriptor"].Value<string>());
            var totalTests = source["succeeded"].Value<int>() +
                             source["succeededWithWarnings"].Value<int>() +
                             source["failed"].Value<int>() +
                             source["notRun"].Value<int>();
            rootResults.SetAttribute("total", totalTests.ToString());
            rootResults.SetAttribute("errors", "0");
            rootResults.SetAttribute("failures", source["failed"].Value<string>());
            rootResults.SetAttribute("not-run", source["notRun"].Value<string>());
            rootResults.SetAttribute("inconclusive", "0");
            rootResults.SetAttribute("ignored", "0");
            rootResults.SetAttribute("skipped", "0");
            rootResults.SetAttribute("invalid", "0");

            var dateSplit = source["reportCreatedOn"].Value<string>().Split("-");
            rootResults.SetAttribute("date", dateSplit[0]);
            rootResults.SetAttribute("time", dateSplit[1]);

            doc.AppendChild(rootResults);

            // Environment
            var environmentElement = doc.CreateElement(string.Empty, "environment", string.Empty);
            environmentElement.SetAttribute("nunit-version", "2.5.8.0");
            rootResults.AppendChild(environmentElement);

            // Culture
            var cultureElement = doc.CreateElement(string.Empty, "culture-info", string.Empty);
            cultureElement.SetAttribute("current-culture", "en-US");
            cultureElement.SetAttribute("current-uiculture", "en-US");
            rootResults.AppendChild(cultureElement);

            // Convert to internal format
            var tests = source["tests"].Values<JToken>();
            var ueTests = new List<UETestResult>(tests.Count());
            foreach (var test in tests) ueTests.Add(new UETestResult(test));

            // UE does a single run so heres our time
            var runTime = source["totalDuration"].Value<string>();

            // Test Suite
            var testSuite = doc.CreateElement(string.Empty, "test-suite", string.Empty);
            testSuite.SetAttribute("type", "Assembly");

            if (!string.IsNullOrEmpty(Suite))
                testSuite.SetAttribute("name", Suite);
            else
                testSuite.SetAttribute("name", "Tests");

            testSuite.SetAttribute("executed", "True");

            if (totalTests == source["succeeded"].Value<int>() +
                source["succeededWithWarnings"].Value<int>())
            {
                testSuite.SetAttribute("result", "Success");
                testSuite.SetAttribute("success", "True");
            }
            else
            {
                testSuite.SetAttribute("result", "Failure");
                testSuite.SetAttribute("success", "False");
            }

            testSuite.SetAttribute("time", runTime);
            testSuite.SetAttribute("asserts", "0");

            rootResults.AppendChild(testSuite);

            var testResults = doc.CreateElement(string.Empty, "results", string.Empty);
            testSuite.AppendChild(testResults);

            foreach (var r in ueTests) testResults.AppendChild(r.GetElement(doc));

            doc.Save(OutputPath);

            return true;
        }

        private class UETestResult
        {
            public readonly string DisplayName;
            public readonly int Errors;
            public readonly string Path;
            public readonly string[] PathTree;

            public readonly string State;

            //  public readonly string[] Messages;
            public readonly int Warnings;
            //public readonly string[] Artifacts;

            public UETestResult(JToken token)
            {
                DisplayName = token["testDisplayName"].Value<string>();
                Path = token["fullTestPath"].Value<string>();
                State = token["state"].Value<string>(); // Success
                Warnings = token["warnings"].Value<int>();
                Errors = token["errors"].Value<int>();

                // entries artifacts

                PathTree = Path.Split(".");
            }

            public XmlElement GetElement(XmlDocument doc)
            {
                var testCase = doc.CreateElement(string.Empty, "test-case", string.Empty);

                testCase.SetAttribute("name", Path);

                testCase.SetAttribute("executed", "True");
                testCase.SetAttribute("time", "0.0");
                testCase.SetAttribute("asserts", "0");

                testCase.SetAttribute("result", State);
                testCase.SetAttribute("success", State == "Success" ? "True" : "False");

                // TODO: Create Failure
                if (State != "Success")
                {
                }

                return testCase;
            }
        }
    }
}