// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

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