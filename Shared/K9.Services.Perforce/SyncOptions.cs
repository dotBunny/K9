// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

namespace K9.Services.Perforce;

public struct SyncOptions
{
    // ReSharper disable UnassignedField.Global
    public int NumberOfRetries;
    public int NumberOfThreads;
    public int TcpBufferSize;
    // ReSharper restore UnassignedField.Global
}
