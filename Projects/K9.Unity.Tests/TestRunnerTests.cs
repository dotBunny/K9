// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using K9.Unity.TestRunner.Report;
using NUnit.Framework;

namespace K9.Unity.Tests
{
    public class TestRunnerTests
    {
        private MemoryStream _testRunPerformanceDataStream;

        [SetUp]
        public void Setup()
        {
            // Get TestRun Performance Data
            Assembly assembly = Assembly.GetExecutingAssembly();
            using Stream stream =
                assembly.GetManifestResourceStream("K9.Unity.Tests.Content.unityTestResults-Performance.xml");
            if (stream == null)
            {
                return;
            }

            using StreamReader reader = new(stream);
            byte[] byteArray = Encoding.ASCII.GetBytes(reader.ReadToEnd());
            _testRunPerformanceDataStream = new MemoryStream(byteArray);
            _testRunPerformanceDataStream.Seek(0, SeekOrigin.Begin);
        }

        [Test]
        public void Parse_TestRunReport()
        {
            _testRunPerformanceDataStream.Seek(0, SeekOrigin.Begin);

            XmlSerializer xml = new(typeof(TestRun), new XmlRootAttribute("test-run"));
            TestRun testRun = (TestRun)xml.Deserialize(_testRunPerformanceDataStream);

            if (testRun == null)
            {
                Assert.Fail("TestRun object is NULL.");
            }
        }

        [Test]
        public void Detect_PerformanceResult()
        {
        }

        [Test]
        public void Detect_TestResult()
        {
        }
    }
}