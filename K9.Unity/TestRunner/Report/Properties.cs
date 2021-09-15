using System.Collections.Generic;
using System.Xml.Serialization;

namespace K9.Unity.TestRunner.Report
{
    [XmlRoot(ElementName = "properties", IsNullable = true)]
    public class Properties
    {
        [XmlElement(ElementName = "property")] public List<Property> Property { get; set; }
    }
}