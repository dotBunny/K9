// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using DocumentFormat.OpenXml.Spreadsheet;
using K9.Reports;
using K9.Reports.Results;

namespace K9.Services.Google
{
    public static class SheetsUtil
    {
        public static void AddRows(this Sheets sheet, IResult result)
        {
            DataTable table = result.GetTable(true);
            foreach (DataRow tableRow in table.Rows)
            {
                sheet.AddRow(result.GetSheetName(), tableRow.ItemArray.ToList());
            }
        }

        public static void UpdateRows(this Sheets sheet, IUpdateableResult result)
        {
            DataTable table = result.GetTable(true);
            foreach (DataRow tableRow in table.Rows)
            {
                sheet.UpdateRow(result.GetSheetName(), result.GetKeyColumn(), result.GetKey(),
                    tableRow.ItemArray.ToList());
            }
        }

        public static string GetGoogleSheetID(this IResult result)
        {
            string category = result.GetCategory();

            if (Core.Settings.Data["GoogleSheets"].ContainsKey(category))
            {
                return Core.Settings.Data["GoogleSheets"][category];
            }

            Log.WriteLine($"Unable to find category: {category} in K9.ini");
            return Core.Settings.Data["GoogleSheets"]["Default"];
        }

        public static bool Post(string configPath, string applicationName, ref List<IResult> results, string documentID = null, bool columnFormat = false)
        {
            // Validate Credentials
            if (!File.Exists(configPath))
            {
                Log.WriteLine($"Unable to find credentials at {configPath}", "GOOGLE");
                return false;
            }

            if (string.IsNullOrEmpty(applicationName))
            {
                Log.WriteLine("A valid ApplicationName is required.", "GOOGLE");
                return false;
            }

            Dictionary<string, List<IResult>> sortedResults = new();
            foreach (IResult r in results)
            {
                if (r != null)
                {
                    string sheetID = documentID ?? r.GetGoogleSheetID();
                    if (!sortedResults.ContainsKey(sheetID))
                    {
                        sortedResults.Add(sheetID, new List<IResult>());
                    }

                    sortedResults[sheetID].Add(r);
                }
            }

            // Build By Sheet
            foreach (KeyValuePair<string, List<IResult>> sheet in sortedResults)
            {
                Sheets google = new(configPath, applicationName, sheet.Key);
                if (!columnFormat)
                {
                    // Build out a row per response
                    foreach (IResult r in sheet.Value)
                    {
                        switch (r.GetResultType())
                        {
                            case ResultType.Updateable:
                                google.UpdateRows((IUpdateableResult)r);
                                break;

                            // By default we are just going to add rows of data
                            default:
                                google.AddRows(r);
                                break;
                        }
                    }
                }
                else
                {
                    // New branch for column based output
                    // Build out a row per response
                    List<object> items = new List<object>();
                    string sheetname = null;

                    if (sheet.Value[0] is UnitTestResult)
                    {
                        UnitTestResult firstResult = (UnitTestResult)sheet.Value[0];
                        items.Add(firstResult.Timestamp.ToString(Core.TimeFormat));
                        items.Add(Core.AgentName);
                        items.Add(Core.Changelist);
                    }

                    foreach (IResult r in sheet.Value)
                    {
                        items.Add(r.GetValue());
                        sheetname = r.GetSheetName();
                    }

                    if (sheetname != null)
                    {
                        google.AddRow(sheetname, items);
                    }
                }

                google.Execute();
            }
            return true;
        }
    }
}