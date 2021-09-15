using CommandLine;

namespace K9
{
    public interface IProgram
    {
        public string DefaultLogCategory { get; }

        // public ParserResult<object> GetResults();
        //
        // public bool ProcessResults(ParserResult<object> results);
    }
}