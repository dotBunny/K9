// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using System.Text.Json;
using System.Xml;
using K9.Core;

namespace K9.Unreal.ToNUnit;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "TONUNIT",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            }, new ToNUnitProvider());

        try
        {
            ToNUnitProvider provider = (ToNUnitProvider)framework.ProgramProvider;

            if (provider.Source == null || provider.Target == null)
            {
                Log.WriteLine("The SOURCE or TARGET is null for an unknown reason.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }


            UnrealTestReport? report = JsonSerializer.Deserialize<UnrealTestReport>(File.ReadAllText(provider.Source));
            if (report == null)
            {
                Log.WriteLine("Unable to parse the provided JSON file.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }


            // Create Header
            XmlDocument doc = new();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", "no");
            XmlElement? root = doc.DocumentElement;
            if (root == null)
            {
                Log.WriteLine("Unable to make XML root for an unknown reason.", ILogOutput.LogType.Error);
                framework.Shutdown();
                return;
            }

            doc.InsertBefore(xmlDeclaration, root);

            // Create Results Root
            XmlElement rootResults = doc.CreateElement(string.Empty, "test-results", string.Empty);
            rootResults.SetAttribute("name", report.ClientDescriptor);


            rootResults.SetAttribute("total", report.TotalTests.ToString());
            rootResults.SetAttribute("errors", "0");
            rootResults.SetAttribute("failures", report.Failed.ToString());
            rootResults.SetAttribute("not-run", report.NotRun.ToString());
            rootResults.SetAttribute("inconclusive", "0");
            rootResults.SetAttribute("ignored", "0");
            rootResults.SetAttribute("skipped", "0");
            rootResults.SetAttribute("invalid", "0");

            if (report.CreatedOn != null)
            {
                string[] dateSplit = report.CreatedOn.Split("-");
                rootResults.SetAttribute("date", dateSplit[0]);
                rootResults.SetAttribute("time", dateSplit[1]);
            }

            doc.AppendChild(rootResults);

            // Environment
            XmlElement environmentElement = doc.CreateElement(string.Empty, "environment", string.Empty);
            environmentElement.SetAttribute("nunit-version", "2.5.8.0");
            rootResults.AppendChild(environmentElement);

            // Culture
            XmlElement cultureElement = doc.CreateElement(string.Empty, "culture-info", string.Empty);
            cultureElement.SetAttribute("current-culture", "en-US");
            cultureElement.SetAttribute("current-uiculture", "en-US");
            rootResults.AppendChild(cultureElement);


            // Test Suite
            XmlElement testSuite = doc.CreateElement(string.Empty, "test-suite", string.Empty);
            testSuite.SetAttribute("type", "Assembly");

            testSuite.SetAttribute("name", !string.IsNullOrEmpty(provider.Suite) ? provider.Suite : "Tests");

            testSuite.SetAttribute("executed", "True");

            if (report.TotalTests == report.Succeeded + report.SucceededWithWarnings)
            {
                testSuite.SetAttribute("result", "Success");
                testSuite.SetAttribute("success", "True");
            }
            else
            {
                testSuite.SetAttribute("result", "Failure");
                testSuite.SetAttribute("success", "False");
            }

            testSuite.SetAttribute("time", report.TotalDuration);
            testSuite.SetAttribute("asserts", "0");

            rootResults.AppendChild(testSuite);

            XmlElement testResults = doc.CreateElement(string.Empty, "results", string.Empty);
            testSuite.AppendChild(testResults);

            foreach (UnrealTestResult r in report.Tests)
            {
                testResults.AppendChild(r.GetElement(doc));
            }

            doc.Save(provider.Target);
        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}