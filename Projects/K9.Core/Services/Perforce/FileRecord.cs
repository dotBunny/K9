// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace K9.Services.Perforce
{
    public class FileRecord
    {
        [Flags]
        public enum FileFlags
        {
            ModTime = 1,
            AlwaysWritable = 2,
            Executable = 4,
            ExclusiveCheckout = 8
        }

        public string Action;
        public string ClientPath;
        public string DepotPath;
        public string Digest;
        public FileFlags Flags;
        public bool IsMapped;
        public string Path;
        public int Revision;
        public bool Unmap;

        public FileRecord(Dictionary<string, string> Tags)
        {
            Tags.TryGetValue("depotFile", out DepotPath);
            Tags.TryGetValue("clientFile", out ClientPath);
            Tags.TryGetValue("path", out Path);
            if (!Tags.TryGetValue("action", out Action))
            {
                Tags.TryGetValue("headAction", out Action);
            }

            string Type;
            if (Tags.TryGetValue("headType", out Type))
            {
                int AttributesIdx = Type.IndexOf('+');
                if (AttributesIdx != -1)
                {
                    for (int Idx = AttributesIdx + 1; Idx < Type.Length; Idx++)
                    {
                        switch (Type[Idx])
                        {
                            case 'm':
                                Flags |= FileFlags.ModTime;
                                break;
                            case 'w':
                                Flags |= FileFlags.AlwaysWritable;
                                break;
                            case 'x':
                                Flags |= FileFlags.Executable;
                                break;
                            case 'l':
                                Flags |= FileFlags.ExclusiveCheckout;
                                break;
                        }
                    }
                }
            }

            IsMapped = Tags.ContainsKey("isMapped");
            Unmap = Tags.ContainsKey("unmap");

            string RevisionString;
            if (Tags.TryGetValue("rev", out RevisionString))
            {
                int.TryParse(RevisionString, out Revision);
            }

            Tags.TryGetValue("digest", out Digest);
        }
    }
}