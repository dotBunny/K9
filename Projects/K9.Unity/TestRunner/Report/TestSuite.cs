﻿// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace K9.Unity.TestRunner.Report
{
    [XmlRoot(ElementName = "test-suite")]
    public class TestSuite
    {
        [XmlAttribute(AttributeName = "type")] public string Type { get; set; }

        [XmlAttribute(AttributeName = "id")] public string Id { get; set; }

        [XmlAttribute(AttributeName = "name")] public string Name { get; set; }

        [XmlAttribute(AttributeName = "fullname")]
        public string FullName { get; set; }

        [XmlAttribute(AttributeName = "classname")]
        public string ClassName { get; set; }

        [XmlAttribute(AttributeName = "runstate")]
        public string RunState { get; set; }

        [XmlAttribute(AttributeName = "testcasecount")]
        public string TestCaseCount { get; set; }

        [XmlAttribute(AttributeName = "result")]
        public string Result { get; set; }

        [XmlAttribute(AttributeName = "start-time")]
        public string StartTime { get; set; }

        [XmlAttribute(AttributeName = "end-time")]
        public string EndTime { get; set; }

        [XmlAttribute(AttributeName = "duration")]
        public string Duration { get; set; }

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

        [XmlElement(ElementName = "properties", IsNullable = true)]
        public Properties Properties { get; set; }

        [XmlElement(ElementName = "test-case")]
        public List<TestCase> TestCases { get; set; }

        [XmlElement(ElementName = "test-suite")]
        public List<TestSuite> TestSuites { get; set; }
    }
}