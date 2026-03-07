// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

namespace K9.Services.Perforce.Records;

public class DescribeFileRecord
{
    public string? Action;
    public string? DepotFile;
    public string? Digest;
    public int FileSize;
    public int Revision;
    public string? Type;
}