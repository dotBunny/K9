// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace K9
{
    public class Commands
    {
        public const string Extension = ".k9.json";

        [JsonPropertyName("actions")] public required CommandVerb[] Actions;

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static Commands? Get(string path)
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Commands>(json);
        }

        public class CommandVerb
        {
            // ReSharper disable UnassignedField.Global
            [JsonPropertyName("verb")] public string? Identifier;
            [JsonPropertyName("command")] public string? Command;
            [JsonPropertyName("workingDirectory")] public string? WorkingDirectory;
            [JsonPropertyName("arguments")] public string? Arguments;
            [JsonPropertyName("description")] public string? Description;
            [JsonPropertyName("actions")] public CommandVerb[]? Actions;
            // ReSharper restore UnassignedField.Global
        }
    }
}
