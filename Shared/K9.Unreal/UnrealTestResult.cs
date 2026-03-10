// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Text.Json.Serialization;
using System.Xml;

namespace K9.Unreal;

public class UnrealTestResult
{
    [JsonPropertyName("testDisplayName")]
    public string? DisplayName;

    [JsonPropertyName("errors")]
    public int Errors;

    [JsonPropertyName("fullTestPath")]
    public string? Path;


    [JsonPropertyName("state")]
    public string? State;

    [JsonPropertyName("warnings")]
    public int Warnings;


    public XmlElement GetElement(XmlDocument doc)
    {
        XmlElement testCase = doc.CreateElement(string.Empty, "test-case", string.Empty);

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