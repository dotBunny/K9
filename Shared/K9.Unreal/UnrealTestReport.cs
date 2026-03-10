// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Text.Json.Serialization;

namespace K9.Unreal;

public class UnrealTestReport
{
    [JsonPropertyName("clientDescriptor")]
    public string? ClientDescriptor;

    [JsonPropertyName("succeeded")]
    public int Succeeded;
    [JsonPropertyName("succeededWithWarnings")]
    public int SucceededWithWarnings;
    [JsonPropertyName("failed")]
    public int Failed;
    [JsonPropertyName("notRun")]
    public int NotRun;

    [JsonPropertyName("totalDuration")]
    public string? TotalDuration;

    [JsonPropertyName("reportCreatedOn")]
    public string? CreatedOn;

    [JsonPropertyName("tests")]
    public UnrealTestResult[] Tests = [];

    public int TotalTests => Succeeded + SucceededWithWarnings + Failed + NotRun;
}

