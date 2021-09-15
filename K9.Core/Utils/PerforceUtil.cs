using System.IO;
using K9.Services.Perforce;

namespace K9.Services.Utils
{
    public static class PerforceUtil
    {
        public static string GetWorkspaceRoot()
        {
#if DEBUG
            // We just assume the local workspace in debug as where the assembly is
            return  Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Core.AssemblyLocation)));
#elif RELEASE
            // This assumes that K9 lives in a folder, one level under the root of the workspace (often CLI)
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Core.AssemblyLocation), ".."));
#endif
        }

        public static bool IsValidTag(string Line, int StartIndex)
        {
            // Annoyingly, we sometimes get commentary with an info1: prefix. Since it typically starts with a depot or file path, we can pick it out.
            for (var Idx = StartIndex; Idx < Line.Length && Line[Idx] != ' '; Idx++)
                if (Line[Idx] == '/' || Line[Idx] == '\\')
                    return false;
            return true;
        }

        public static bool IgnoreCommandOutput(string Text, P4.CommandOptions Options)
        {
            if (Text.StartsWith("exit: ") || Text.StartsWith("info2: ") || Text.Length == 0) return true;
            if (Options.HasFlag(P4.CommandOptions.IgnoreFilesUpToDateError) && Text.StartsWith("error: ") &&
                Text.EndsWith("- file(s) up-to-date.")) return true;
            if (Options.HasFlag(P4.CommandOptions.IgnoreNoSuchFilesError) && Text.StartsWith("error: ") &&
                Text.EndsWith(" - no such file(s).")) return true;
            if (Options.HasFlag(P4.CommandOptions.IgnoreFilesNotInClientViewError) && Text.StartsWith("error: ") &&
                Text.EndsWith("- file(s) not in client view.")) return true;
            if (Options.HasFlag(P4.CommandOptions.IgnoreFilesNotOnClientError) && Text.StartsWith("error: ") &&
                Text.EndsWith("- file(s) not on client.")) return true;
            if (Options.HasFlag(P4.CommandOptions.IgnoreFilesNotOpenedOnThisClientError) &&
                Text.StartsWith("error: ") && Text.EndsWith(" - file(s) not opened on this client.")) return true;
            if (Options.HasFlag(P4.CommandOptions.IgnoreProtectedNamespaceError) && Text.StartsWith("error: ") &&
                Text.EndsWith(" - protected namespace - access denied.")) return true;
            if (Options.HasFlag(P4.CommandOptions.IgnoreEnterPassword) && Text.StartsWith("Enter password:"))
                return true;

            return false;
        }

        public static bool TryGetDepotName(string DepotPath, out string DepotName)
        {
            return TryGetClientName(DepotPath, out DepotName);
        }

        public static bool TryGetClientName(string ClientPath, out string ClientName)
        {
            if (!ClientPath.StartsWith("//"))
            {
                ClientName = null;
                return false;
            }

            var SlashIdx = ClientPath.IndexOf('/', 2);
            if (SlashIdx == -1)
            {
                ClientName = null;
                return false;
            }

            ClientName = ClientPath.Substring(2, SlashIdx - 2);
            return true;
        }

        public static string GetClientOrDepotDirectoryName(string ClientFile)
        {
            var Index = ClientFile.LastIndexOf('/');
            if (Index == -1) return "";

            return ClientFile.Substring(0, Index);
        }

        public static string EscapePath(string Path)
        {
            var NewPath = Path;
            NewPath = NewPath.Replace("%", "%25");
            NewPath = NewPath.Replace("*", "%2A");
            NewPath = NewPath.Replace("#", "%23");
            NewPath = NewPath.Replace("@", "%40");
            return NewPath;
        }

        public static string UnescapePath(string Path)
        {
            var NewPath = Path;
            NewPath = NewPath.Replace("%40", "@");
            NewPath = NewPath.Replace("%23", "#");
            NewPath = NewPath.Replace("%2A", "*");
            NewPath = NewPath.Replace("%2a", "*");
            NewPath = NewPath.Replace("%25", "%");
            return NewPath;
        }

        public static bool ParseCommandOutput(string Text, P4.HandleOutputDelegate HandleOutput,
            P4.CommandOptions Options)
        {
            if (Options.HasFlag(P4.CommandOptions.NoChannels))
            {
                var Line = new OutputLine(OutputChannel.Unknown, Text);
                return HandleOutput(Line);
            }

            if (!IgnoreCommandOutput(Text, Options))
            {
                OutputLine Line;
                if (Text.StartsWith("text: "))
                    Line = new OutputLine(OutputChannel.Text, Text.Substring(6));
                else if (Text.StartsWith("info: "))
                    Line = new OutputLine(OutputChannel.Info, Text.Substring(6));
                else if (Text.StartsWith("info1: "))
                    Line = new OutputLine(IsValidTag(Text, 7) ? OutputChannel.TaggedInfo : OutputChannel.Info,
                        Text.Substring(7));
                else if (Text.StartsWith("warning: "))
                    Line = new OutputLine(OutputChannel.Warning, Text.Substring(9));
                else if (Text.StartsWith("error: "))
                    Line = new OutputLine(OutputChannel.Error, Text.Substring(7));
                else
                    Line = new OutputLine(OutputChannel.Unknown, Text);

                return HandleOutput(Line) &&
                       (Line.Channel != OutputChannel.Error || Options.HasFlag(P4.CommandOptions.NoFailOnErrors)) &&
                       Line.Channel != OutputChannel.Unknown;
            }

            return true;
        }
    }
}