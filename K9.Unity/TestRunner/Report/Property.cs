using System.Xml.Serialization;

namespace K9.Unity.TestRunner.Report
{
    [XmlRoot(ElementName = "property", IsNullable = true)]
    public class Property
    {
        [XmlAttribute(AttributeName = "name")] public string Name { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }
}