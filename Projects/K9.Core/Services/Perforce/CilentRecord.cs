// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace K9.Services.Perforce
{
    public class ClientRecord
    {
        public string Host;
        public string Name;
        public string Owner;
        public string Root;
        public string Stream;

        public ClientRecord(Dictionary<string, string> Tags)
        {
            Tags.TryGetValue("client", out Name);
            Tags.TryGetValue("Owner", out Owner);
            Tags.TryGetValue("Host", out Host);
            Tags.TryGetValue("Stream", out Stream);
            Tags.TryGetValue("Root", out Root);
        }
    }
}