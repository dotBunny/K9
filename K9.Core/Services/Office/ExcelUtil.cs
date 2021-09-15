using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using K9.Reports;

namespace K9.Services.Office
{
    public static class ExcelUtil
    {
        public static void Post(string baseFolder, ref List<IResult> results)
        {
            var sortedResults = new Dictionary<string, List<IResult>>();
            foreach (var r in results)
            {
                var reporter = r;
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
            
            var workbooks = new Dictionary<string, XLWorkbook>();
            foreach (var key in sortedResults.Keys)
            {
                var path = Path.Combine(baseFolder, key + ".xlsx");
                if (File.Exists(path))
                {
                    workbooks[key] = new XLWorkbook(path);
                }
                else
                {
                    var newWorkbook = new XLWorkbook();
                    newWorkbook.AddWorksheet("Data");
                    newWorkbook.SaveAs(path);
                    workbooks[key] = newWorkbook;
                }
            }

            foreach (var sheet in sortedResults)
            {
                // Make sure we have a "Data" worksheet
                if (!workbooks[sheet.Key].Worksheets.Contains("Data"))
                {
                    workbooks[sheet.Key].AddWorksheet("Data");
                }
                var worksheet = workbooks[sheet.Key].Worksheet("Data");
                
                // Get our row to start inserting at
                var currentRow = 1;
                if (worksheet.LastRowUsed() != null)
                {
                    currentRow = worksheet.LastRowUsed().RowNumber() + 1;
                }

                // TODO: This is how we could wrap the data
                if (currentRow > 1000000)
                {
                    Log.WriteLine("Workbook is too close to the maximum number of rows to process.", "ERROR");
                }
                
                foreach (var r in sheet.Value)
                {
                    // Create insertion table
                    var table = r.GetTable();
                    worksheet.Cell(currentRow, 1).InsertData(table);
                    currentRow += table.Rows.Count;
                }
            }

            // Save all workbooks, and close them
            foreach (var wb in workbooks)
            {
                wb.Value.Save();
            }
        }
    }
}