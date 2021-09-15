using System;
using System.Data;

namespace K9.Reports.Results
{
    public class GenericResult : IResult
    {
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

        public override string ToString()
        {
            return GetName();
        }
    }
}