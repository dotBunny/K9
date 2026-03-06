// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

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