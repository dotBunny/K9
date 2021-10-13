// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace K9.Reports.Results
{
    public class PerformanceResult : IResult
    {
        public List<PerformanceTestResultSample> Samples = new();

        public DateTime Timestamp { get; set; }
        public string FullName { get; set; }
        public string Category { get; set; }
        public Agent Runner { get; set; }


        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append("==> ");
            sb.AppendLine(FullName);
            foreach (PerformanceTestResultSample s in Samples)
            {
                sb.AppendLine(s.ToString());
            }

            return sb.ToString();
        }


        public class PerformanceTestResultSample
        {
            public float Average;
            public float Maximum;
            public float Median;
            public float Minimum;
            public string Name;
            public int SampleCount;
            public float StandardDeviation;
            public float Sum;

            public override string ToString()
            {
                return
                    $"[{Name}] Median: {Median}ns | Minimum: {Minimum}ns | Maximum: {Maximum}ns | Average: {Average}ns | Standard Deviation: {StandardDeviation}ns | Sample Count: {SampleCount} | Total Time: {Sum}ns";
            }

            public void AddDataRow(DataTable table, PerformanceResult result, bool objectsAsStrings = false)
            {
                table.Rows.Add(
                    objectsAsStrings ? result.Timestamp.ToString(Core.TimeFormat) : result.Timestamp,
                    result.Runner.Name,
                    Core.Changelist,
                    $"{result.GetName()}.{Name}",
                    Median,
                    Minimum,
                    Maximum,
                    Average,
                    StandardDeviation,
                    SampleCount,
                    Sum);
            }
        }

        #region IResult

        public string GetSheetName()
        {
            return Runner.Name;
        }

        public string GetCategory()
        {
            return Category;
        }

        public ResultType GetResultType()
        {
            return ResultType.Measurement;
        }

        public string GetName()
        {
            return FullName;
        }

        public DataTable GetTable(bool objectsAsStrings = false)
        {
            DataTable table = new();

            table.Columns.Add("Timestamp", objectsAsStrings ? typeof(string) : typeof(DateTime));
            table.Columns.Add("Agent", typeof(string));
            table.Columns.Add("Changelist", typeof(int));
            table.Columns.Add("Sample", typeof(string));
            table.Columns.Add("Median", typeof(float));
            table.Columns.Add("Minimum", typeof(float));
            table.Columns.Add("Maximum", typeof(float));
            table.Columns.Add("Average", typeof(float));
            table.Columns.Add("Standard Deviation", typeof(float));
            table.Columns.Add("Sample Count", typeof(int));
            table.Columns.Add("Sum", typeof(float));

            foreach (PerformanceTestResultSample s in Samples)
            {
                s.AddDataRow(table, this, objectsAsStrings);
            }

            return table;
        }

        #endregion
    }
}