using System.Collections.Generic;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Sheet = Google.Apis.Sheets.v4.Data.Sheet;
using SheetProperties = Google.Apis.Sheets.v4.Data.SheetProperties;

namespace K9.Services.Google
{
    public class Sheets
    {
        public const int CellLimit = 5000000;
        public const string LogChannel = "GOOGLE";


        private readonly Dictionary<string, CachedAddData> _addValueCache;
        private readonly Dictionary<string, List<CachedUpdateData>> _updateValueCache;
        private readonly Dictionary<string, IList<IList<object>>> _existingDataCache;


        private string _applicationName;

        private readonly ServiceAccountCredential _googleCredential;
        private readonly SheetsService _sheetService;
        private readonly string _spreadsheetID;

        public Spreadsheet GetSpreadsheet()
        {
            var request = _sheetService.Spreadsheets.Get(_spreadsheetID);
            return request.Execute();
        }

        public Sheets(string credentialsPath, string ApplicationName, string SpreadsheetID)
        {
            _applicationName = ApplicationName;
            _spreadsheetID = SpreadsheetID;
            _addValueCache = new Dictionary<string, CachedAddData>();
            _updateValueCache = new Dictionary<string, List<CachedUpdateData>>();
            _existingDataCache = new Dictionary<string, IList<IList<object>>>();

            // Get Credentials
            using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
            _googleCredential = GoogleCredential.FromStream(stream)
                .CreateScoped(SheetsService.Scope.Spreadsheets)
                .UnderlyingCredential as ServiceAccountCredential;

            // Do we haz credentials?
            HasCredential = _googleCredential != null;
            if (!HasCredential)
            {
                Log.WriteLine("Failed to get credentials.", LogChannel);
                return;
            }

            // Setup Service
            _sheetService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _googleCredential,
                ApplicationName = ApplicationName
            });
            HasService = _sheetService != null;
            if (HasService) return;
            Log.WriteLine("Failed to create service.", LogChannel);
        }

        public bool HasCredential { get; }
        public bool HasService { get; }

       
        public void AddRow(string sheetName, List<object> rowData)
        {
            if (!_addValueCache.ContainsKey(sheetName))
            {
                _addValueCache.Add(sheetName, new CachedAddData());
                _addValueCache[sheetName].Data = new ValueRange();
                _addValueCache[sheetName].Data.Values = new List<IList<object>>();
            }

            if (rowData.Count > _addValueCache[sheetName].MaxColumns) _addValueCache[sheetName].MaxColumns = rowData.Count;

            _addValueCache[sheetName].Data.Values.Add(rowData);
        }

        public bool RemoveRowImmediately(int startRowIndex, int numberOfRows)
        {
            var batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest();
            batchUpdateSpreadsheetRequest.Requests = new List<Request>();

           
            var request = new Request();
            
            // request.DeleteDimension = new DeleteDimensionRequest
            // {
            //     Range = new DimensionRange
            //     {
            //         Dimension = "ROWS", 
            //         StartIndex = startRowIndex, 
            //         EndIndex = startRowIndex + numberOfRows
            //     }
            // };
            
            request.DeleteDimensionGroup = new DeleteDimensionGroupRequest
            {
                Range = new DimensionRange
                {
                    Dimension = "ROWS", 
                    StartIndex = startRowIndex, 
                    EndIndex = startRowIndex + numberOfRows
                }
            };

            batchUpdateSpreadsheetRequest.Requests.Add(request);
            
            var result = _sheetService.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, _spreadsheetID).Execute();
            return result.Replies.Count == 1 && 
                   result.Replies[0].DeleteDimensionGroup != null;
        }
        
        public void UpdateRow(string sheetName, string keyColumn, string identifier, List<object> rowData)
        {
            if (!_updateValueCache.ContainsKey(sheetName))
            {
                _updateValueCache.Add(sheetName, new List<CachedUpdateData>());
            }
            
            string column = keyColumn.ToUpper();
            char[] charValue = column.ToCharArray(0, 1);
            
            _updateValueCache[sheetName].Add(new CachedUpdateData()
            {
                Column = column,
                ColumnInteger =  (int) charValue[0] - 65,
                Key = identifier,
                Data = rowData,
                MaxColumns = rowData.Count
            });
        }


        public List<string> GetExistingSheetNames()
        {
            List<string> returnList = new List<string>();
            foreach(Sheet sheet in GetSpreadsheet().Sheets)
            {
                returnList.Add(sheet.Properties.Title);
            }
            return returnList;
        }

        
        public bool CreateSheetImmediately(string name)
        {
            var batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest();
            batchUpdateSpreadsheetRequest.Requests = new List<Request>();
            
            var addSheetRequest = new AddSheetRequest();
            addSheetRequest.Properties = new SheetProperties {Title = name};

            var request = new Request {AddSheet = addSheetRequest};
            batchUpdateSpreadsheetRequest.Requests.Add(request);

            var result = _sheetService.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, _spreadsheetID).Execute();
            return result.Replies.Count == 1 && result.Replies[0].AddSheet != null &&
                   result.Replies[0].AddSheet.Properties.Title == name;
        }

        public IList<IList<object>> GetSheetData(string sheetName)
        {
            var request = _sheetService.Spreadsheets.Values.Get(_spreadsheetID, $"{sheetName}!1:{GetRowCount(sheetName)}");
            var response = request.Execute();
            return response.Values;
        }

        public int GetRowCount(string sheetName)
        {
            foreach (Sheet sheet in GetSpreadsheet().Sheets)
            {
                if (sheet != null && sheet.Properties.Title == sheetName && sheet.Properties.GridProperties.RowCount != null)
                {
                    return (int)sheet.Properties.GridProperties.RowCount;
                }
            }
            return 0;
        }


        public bool Execute()
        {
            if (!HasCredential || !HasService)
            {
                Log.WriteLine("Sheets API has not been initialized correctly.", LogChannel);
                return false;
            }

            // Cache our existing sheets
            List<string> existingSheets = GetExistingSheetNames();

            // Process Add Calls
            if (_addValueCache.Count > 0)
            {
                Log.WriteLine($"Adding {_addValueCache.Count} Rows.", LogChannel);
                foreach (var entry in _addValueCache)
                {
                    if (!existingSheets.Contains(entry.Key))
                    {
                        Log.WriteLine($"Creating Sheet {entry.Key}.", LogChannel);
                        if (CreateSheetImmediately(entry.Key)) existingSheets.Add(entry.Key);
                    }

                    var appendRequest = _sheetService.Spreadsheets.Values.Append(entry.Value.Data, _spreadsheetID,
                        $"{entry.Key}!A:{char.ConvertFromUtf32(64 + entry.Value.MaxColumns)}");
                    appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest
                        .ValueInputOptionEnum.USERENTERED;
                    appendRequest.Execute();
                }
                _addValueCache.Clear();
            }

            // Process Update Calls
            if (_updateValueCache.Count > 0)
            {
                // Get Copies Of Existing DataTables (sigh)
                foreach (var entry in _updateValueCache)
                {
                    if (!existingSheets.Contains(entry.Key))
                    {
                        Log.WriteLine($"Creating Sheet {entry.Key}.", LogChannel);
                        if (CreateSheetImmediately(entry.Key)) existingSheets.Add(entry.Key);
                    }
                    
                    if (!_existingDataCache.ContainsKey(entry.Key))
                    {
                        _existingDataCache.Add(entry.Key, GetSheetData(entry.Key));
                    }

                    var batchUpdateRequest = new BatchUpdateValuesRequest();
                    batchUpdateRequest.Data = new List<ValueRange>();
                    batchUpdateRequest.ValueInputOption = "USER_ENTERED";

                    // Find Rows For Updates
                    for (int i = 0; i < _updateValueCache[entry.Key].Count; i++)
                    {
                        var lookingForID = _updateValueCache[entry.Key][i].Key;

                        if (_existingDataCache.ContainsKey(entry.Key) && _existingDataCache[entry.Key] != null)
                        {
                            for (int rowID = 0; rowID < _existingDataCache[entry.Key].Count; rowID++)
                            {
                                var row = _existingDataCache[entry.Key][rowID][
                                    _updateValueCache[entry.Key][i].ColumnInteger];
                                if ((string) row == lookingForID)
                                {
                                    _updateValueCache[entry.Key][i].RowNumber = rowID + 1;
                                }
                            }
                        }

                        // We need to add a row instead
                        ValueRange valueRange = new ValueRange()
                        {
                            Values = new List<IList<object>> { _updateValueCache[entry.Key][i].Data }
                        };
                        
                        // Found Existing
                        if (_updateValueCache[entry.Key][i].RowNumber != -1)
                        {
                            //valueRange.MajorDimension = "COLUMNS";//"ROWS";//COLUMNS

                            // Send Update
                            valueRange.Range =
                                $"{entry.Key}!A{_updateValueCache[entry.Key][i].RowNumber}:{char.ConvertFromUtf32(64 + _updateValueCache[entry.Key][i].MaxColumns)}{_updateValueCache[entry.Key][i].RowNumber}";
                            batchUpdateRequest.Data.Add(valueRange);
                        }
                        else
                        {
                            var appendRequest = _sheetService.Spreadsheets.Values.Append(valueRange, _spreadsheetID,
                                $"{entry.Key}!A:{char.ConvertFromUtf32(64 + _updateValueCache[entry.Key][i].MaxColumns)}");
                            
                            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest
                                .ValueInputOptionEnum.USERENTERED;
                            appendRequest.Execute();
                        }
                    }

                    // Batch Update
                    if (batchUpdateRequest.Data.Count > 0)
                    {
                        var batchRequest = _sheetService.Spreadsheets.Values.BatchUpdate(batchUpdateRequest, _spreadsheetID);
                        batchRequest.Execute();
                    }
                }
            }
            return true;
        }

        private class CachedAddData
        {
            public ValueRange Data;
            public int MaxColumns;
        }

        private class CachedUpdateData
        {
            public string Key;
            
            public string Column;
            
            public int ColumnInteger;
           
            public int RowNumber = -1; // Not Found
            //public string LastGood;

            public List<object> Data;
            
            public int MaxColumns;
        }
    }
}