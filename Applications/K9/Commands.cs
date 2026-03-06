// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace K9
{   
    public class Commands
    {
        public const string Extension = ".k9.json";

        [JsonPropertyName("actions")]
        public required CommandVerb[] Actions { get; set; }

        public string ToJson()
        {
            return JsonSerializer.Serialize<Commands>(this);
        }

        public static Commands? Get(string path)
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Commands>(json);
        }

        public class CommandVerb
        {
            [JsonPropertyName("verb")]
            public string? Identifier { get; set; }

            [JsonPropertyName("command")]
            public string? Command { get; set; }

            [JsonPropertyName("workingDirectory")]
            public string? WorkingDirectory { get; set; }

            [JsonPropertyName("arguments")]
            public string? Arguments { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("actions")]
            public CommandVerb[]? Actions { get; set; }
        }
    }
}
