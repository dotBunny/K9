namespace K9.Reports
{
    public interface IUpdateableResult : IResult
    {
        string GetKey();
        string GetKeyColumn();
    }
}