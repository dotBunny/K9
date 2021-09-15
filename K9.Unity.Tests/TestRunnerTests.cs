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
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("K9.Unity.Tests.Content.testrun-performance.xml");
            using var reader = new StreamReader(stream);
            var byteArray = Encoding.ASCII.GetBytes(reader.ReadToEnd());
            _testRunPerformanceDataStream = new MemoryStream(byteArray);
            _testRunPerformanceDataStream.Seek(0, SeekOrigin.Begin);
        }

        [Test]
        public void Parse_TestRunReport()
        {
            _testRunPerformanceDataStream.Seek(0, SeekOrigin.Begin);

            var xml = new XmlSerializer(typeof(TestRun), new XmlRootAttribute("test-run"));
            var testRun = (TestRun) xml.Deserialize(_testRunPerformanceDataStream);

            if (testRun == null) Assert.Fail("TestRun object is NULL.");
        }

        [Test]
        public void Detect_PerformanceResult()
        {
            //Assert.Pass();
        }

        [Test]
        public void Detect_TestResult()
        {
        }
    }
}