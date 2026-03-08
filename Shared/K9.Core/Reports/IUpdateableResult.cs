// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

namespace K9.Core.Reports;

public interface IUpdateableResult : IResult
{
    string GetKey();
    string GetKeyColumn();
}