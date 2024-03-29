﻿// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Data;

namespace K9.Reports.Results
{
    public class UnitTestResult : IUpdateableResult
    {
        public DateTime Timestamp { get; set; }
        public string Category { get; set; }
        public string Suite { get; set; }
        public string FullName { get; set; }
        public string Result { get; set; }
        public float Duration { get; set; }


        public override string ToString()
        {
            return $"[{Result}]\t{FullName} ({Duration})";
        }

        #region IResult

        public string GetSheetName()
        {
            return !string.IsNullOrEmpty(Suite) ? $"Unit Tests ({Core.Platform}-{Suite})" : $"Unit Tests ({Core.Platform})";
        }

        public string GetCategory()
        {
            return Category;
        }

        public ResultType GetResultType()
        {
            return ResultType.Updateable;
        }

        public string GetName()
        {
            return FullName;
        }

        public DataTable GetTable(bool objectsAsStrings = false)
        {
            DataTable table = new();

            table.Columns.Add("Timestamp", objectsAsStrings ? typeof(string) : typeof(DateTime));
            table.Columns.Add("AgentName", typeof(string));
            table.Columns.Add("Changelist", typeof(string));
            table.Columns.Add("Test", typeof(string));
            table.Columns.Add("Result", typeof(string));
            table.Columns.Add("Duration", typeof(float));
            // LAST GOOD

            if (objectsAsStrings)
            {
                table.Rows.Add(Timestamp.ToString(Core.TimeFormat), Core.AgentName, Core.Changelist, FullName, Result, Duration);
            }
            else
            {
                table.Rows.Add(Timestamp, Core.AgentName, Core.Changelist, FullName, Result, Duration);
            }

            return table;
        }

        public object GetValue()
        {
            return Duration;
        }

        public Type GetValueType()
        {
            return typeof(float);
        }

        #endregion

        #region IPassFailResult

        public string GetKey()
        {
            return FullName;
        }

        public string GetKeyColumn()
        {
            return "C";
        }

        #endregion
    }
}