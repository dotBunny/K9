// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

namespace K9.Services.Perforce
{
    public class SyncOptions
    {
        public int NumRetries;
        public int NumThreads;
        public int TcpBufferSize;

        public SyncOptions Clone()
        {
            return (SyncOptions)MemberwiseClone();
        }
    }
}