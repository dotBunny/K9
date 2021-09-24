using System.Collections.Generic;
using System.Data;
using System.IO;
using ClosedXML.Excel;
using K9.Reports;

namespace K9.Services.Office
{
    public static class ExcelUtil
    {
        public static void Post(string baseFolder, ref List<IResult> results)
        {
            Dictionary<string, List<IResult>> sortedResults = new();
            foreach (IResult r in results)
            {
                IResult reporter = r;
                if (reporter != null)
                {
                    string cat = reporter.GetCategory();
                    if (!sortedResults.ContainsKey(cat))
                    {
                        sortedResults.Add(cat, new List<IResult>());
                    }

                    sortedResults[cat].Add(reporter);
                }
            }

            Dictionary<string, XLWorkbook> workbooks = new();
            foreach (string key in sortedResults.Keys)
            {
                string path = Path.Combine(baseFolder, key + ".xlsx");
                if (File.Exists(path))
                {
                    workbooks[key] = new XLWorkbook(path);
                }
                else
                {
                    XLWorkbook newWorkbook = new();
                    newWorkbook.AddWorksheet("Data");
                    newWorkbook.SaveAs(path);
                    workbooks[key] = newWorkbook;
                }
            }

            foreach (KeyValuePair<string, List<IResult>> sheet in sortedResults)
            {
                // Make sure we have a "Data" worksheet
                if (!workbooks[sheet.Key].Worksheets.Contains("Data"))
                {
                    workbooks[sheet.Key].AddWorksheet("Data");
                }

                IXLWorksheet worksheet = workbooks[sheet.Key].Worksheet("Data");

                // Get our row to start inserting at
                int currentRow = 1;
                if (worksheet.LastRowUsed() != null)
                {
                    currentRow = worksheet.LastRowUsed().RowNumber() + 1;
                }

                // TODO: This is how we could wrap the data
                if (currentRow > 1000000)
                {
                    Log.WriteLine("Workbook is too close to the maximum number of rows to process.", "ERROR");
                }

                foreach (IResult r in sheet.Value)
                {
                    // Create insertion table
                    DataTable table = r.GetTable();
                    worksheet.Cell(currentRow, 1).InsertData(table);
                    currentRow += table.Rows.Count;
                }
            }

            // Save all workbooks, and close them
            foreach (KeyValuePair<string, XLWorkbook> wb in workbooks)
            {
                wb.Value.Save();
            }
        }
    }
}