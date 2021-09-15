using CommandLine;
using CommandLine.Text;

namespace K9.Utils
{
    public static class CommandLineUtil
    {
        public static void HandleParserResults<T>(ParserResult<T> results)
        {
            if (results.Tag == ParserResultType.NotParsed)
                results.WithNotParsed(v =>
                {
                    foreach (var e in v)
                        if (e.Tag == ErrorType.HelpRequestedError || e.Tag == ErrorType.HelpVerbRequestedError)
                            Log.Write(HelpText.AutoBuild(results, _ => _, _ => _));
                        else
                        {
                            Log.Write($"{e.Tag} - No actions taken.");
                        }
                });
        }
    }
}