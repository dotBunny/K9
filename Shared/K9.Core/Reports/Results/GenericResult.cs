// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Data;

namespace K9.Core.Reports.Results;

public class GenericResult : IResult
{
    public override string ToString()
    {
        return GetName();
    }

    #region IResult

    public string GetSheetName()
    {
        return "Default";
    }

    public string GetCategory()
    {
        return "None";
    }

    public ResultType GetResultType()
    {
        return ResultType.Generic;
    }


    public string GetName()
    {
        return "Generic Result";
    }

    public object GetValue()
    {
        return "";
    }

    public Type GetValueType()
    {
        return typeof(string);
    }

    public DataTable GetTable(bool objectsAsStrings = false)
    {
        return new DataTable();
    }

    #endregion
}