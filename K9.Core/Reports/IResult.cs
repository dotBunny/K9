using System.Data;

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
    }
}