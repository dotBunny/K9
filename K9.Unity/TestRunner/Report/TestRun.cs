using System.Xml.Serialization;

namespace K9.Unity.TestRunner.Report
{
    [XmlRoot(ElementName = "test-run")]
    public class TestRun
    {
        [XmlAttribute(AttributeName = "id")] public int Id { get; set; }

        [XmlAttribute(AttributeName = "testcasecount")]
        public int TestCaseCount { get; set; }

        [XmlAttribute(AttributeName = "result")]
        public string Result { get; set; }

        [XmlAttribute(AttributeName = "total")]
        public int Total { get; set; }

        [XmlAttribute(AttributeName = "passed")]
        public int Passed { get; set; }

        [XmlAttribute(AttributeName = "failed")]
        public int Failed { get; set; }

        [XmlAttribute(AttributeName = "inconclusive")]
        public int Inconclusive { get; set; }

        [XmlAttribute(AttributeName = "skipped")]
        public int Skipped { get; set; }

        [XmlAttribute(AttributeName = "asserts")]
        public int Asserts { get; set; }

        [XmlAttribute(AttributeName = "engine-version")]
        public string EngineVersion { get; set; }

        [XmlAttribute(AttributeName = "clr-version")]
        public string CLRVersion { get; set; }

        [XmlAttribute(AttributeName = "start-time")]
        public string StartTime { get; set; }

        [XmlAttribute(AttributeName = "end-time")]
        public string EndTime { get; set; }

        [XmlAttribute(AttributeName = "duration")]
        public float Duration { get; set; }

        [XmlElement(ElementName = "test-suite")]
        public TestSuite TestSuite { get; set; }
    }
}