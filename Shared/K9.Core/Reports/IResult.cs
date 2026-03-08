// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Data;

namespace K9.Core.Reports;

public enum ResultType
{
    Generic = 0,
    Updateable = 1,
    Measurement = 2
}

public interface IResult
{
    string GetCategory();
    string GetName();
    ResultType GetResultType();
    string GetSheetName();
    DataTable GetTable(bool objectsAsStrings = false);

    object GetValue();
    Type GetValueType();
}