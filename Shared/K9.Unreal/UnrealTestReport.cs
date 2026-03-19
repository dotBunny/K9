// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Text.Json.Serialization;

namespace K9.Unreal;

public class UnrealTestReport
{
    [JsonPropertyName("clientDescriptor")]
    public string? ClientDescriptor { get; set; }

    [JsonPropertyName("succeeded")]
    public int Succeeded { get; set; }
    [JsonPropertyName("succeededWithWarnings")]
    public int SucceededWithWarnings { get; set; }
    [JsonPropertyName("failed")]
    public int Failed { get; set; }
    [JsonPropertyName("notRun")]
    public int NotRun { get; set; }

    [JsonPropertyName("totalDuration")]
    public string? TotalDuration { get; set; }

    [JsonPropertyName("reportCreatedOn")]
    public string? CreatedOn { get; set; }

    [JsonPropertyName("tests")]
    public UnrealTestResult[] Tests { get; set; } = [];

    public int TotalTests => Succeeded + SucceededWithWarnings + Failed + NotRun;
}

