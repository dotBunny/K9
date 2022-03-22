// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Data;
using System.Runtime.CompilerServices;

namespace K9.Reports
{
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
}