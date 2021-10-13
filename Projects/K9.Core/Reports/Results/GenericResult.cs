// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Data;

namespace K9.Reports.Results
{
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

        public DataTable GetTable(bool objectsAsStrings = false)
        {
            return new DataTable();
        }

        #endregion
    }
}