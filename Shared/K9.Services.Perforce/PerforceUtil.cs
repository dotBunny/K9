// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

namespace K9.Services.Perforce;

public static class PerforceUtil
{

    static bool IsValidTag(string line, int startIndex)
    {
        // Annoyingly, we sometimes get commentary with an info1: prefix. Since it typically starts with a depot or file path, we can pick it out.
        for (int index = startIndex; index < line.Length && line[index] != ' '; index++)
        {
            if (line[index] == '/' || line[index] == '\\')
            {
                return false;
            }
        }

        return true;
    }

    static bool IgnoreCommandOutput(string text, PerforceProvider.CommandOptions options)
    {
        if (text.StartsWith("exit: ") || text.StartsWith("info2: ") || text.Length == 0)
        {
            return true;
        }

        if (options.HasFlag(PerforceProvider.CommandOptions.IgnoreFilesUpToDateError) && text.StartsWith("error: ") &&
            text.EndsWith("- file(s) up-to-date."))
        {
            return true;
        }

        if (options.HasFlag(PerforceProvider.CommandOptions.IgnoreNoSuchFilesError) && text.StartsWith("error: ") &&
            text.EndsWith(" - no such file(s)."))
        {
            return true;
        }

        if (options.HasFlag(PerforceProvider.CommandOptions.IgnoreFilesNotInClientViewError) &&
            text.StartsWith("error: ") &&
            text.EndsWith("- file(s) not in client view."))
        {
            return true;
        }

        if (options.HasFlag(PerforceProvider.CommandOptions.IgnoreFilesNotOnClientError) &&
            text.StartsWith("error: ") &&
            text.EndsWith("- file(s) not on client."))
        {
            return true;
        }

        if (options.HasFlag(PerforceProvider.CommandOptions.IgnoreFilesNotOpenedOnThisClientError) &&
            text.StartsWith("error: ") && text.EndsWith(" - file(s) not opened on this client."))
        {
            return true;
        }

        if (options.HasFlag(PerforceProvider.CommandOptions.IgnoreProtectedNamespaceError) &&
            text.StartsWith("error: ") &&
            text.EndsWith(" - protected namespace - access denied."))
        {
            return true;
        }

        if (options.HasFlag(PerforceProvider.CommandOptions.IgnoreEnterPassword) && text.StartsWith("Enter password:"))
        {
            return true;
        }

        return false;
    }

    public static bool TryGetDepotName(string depotPath, out string? depotName)
    {
        return TryGetClientName(depotPath, out depotName);
    }

    static bool TryGetClientName(string clientPath, out string? clientName)
    {
        if (!clientPath.StartsWith("//"))
        {
            clientName = null;
            return false;
        }

        int slashIndex = clientPath.IndexOf('/', 2);
        if (slashIndex == -1)
        {
            clientName = null;
            return false;
        }

        clientName = clientPath[2..slashIndex];
        return true;
    }

    public static string GetClientOrDepotDirectoryName(string clientFile)
    {
        int index = clientFile.LastIndexOf('/');
        if (index == -1)
        {
            return "";
        }

        return clientFile[..index];
    }

    public static string EscapePath(string path)
    {
        string newPath = path;
        newPath = newPath.Replace("%", "%25");
        newPath = newPath.Replace("*", "%2A");
        newPath = newPath.Replace("#", "%23");
        newPath = newPath.Replace("@", "%40");
        return newPath;
    }

    public static string UnescapePath(string path)
    {
        string newPath = path;
        newPath = newPath.Replace("%40", "@");
        newPath = newPath.Replace("%23", "#");
        newPath = newPath.Replace("%2A", "*");
        newPath = newPath.Replace("%2a", "*");
        newPath = newPath.Replace("%25", "%");
        return newPath;
    }

    public static bool ParseCommandOutput(string text, PerforceProvider.HandleOutputDelegate handleOutput,
        PerforceProvider.CommandOptions options)
    {
        if (options.HasFlag(PerforceProvider.CommandOptions.NoChannels))
        {
            OutputLine line = new(OutputLine.OutputChannel.Unknown, text);
            return handleOutput(line);
        }

        if (!IgnoreCommandOutput(text, options))
        {
            OutputLine line;
            if (text.StartsWith("text: "))
            {
                line = new OutputLine(OutputLine.OutputChannel.Text, text[6..]);
            }
            else if (text.StartsWith("info: "))
            {
                line = new OutputLine(OutputLine.OutputChannel.Info, text[6..]);
            }
            else if (text.StartsWith("info1: "))
            {
                line = new OutputLine(
                    IsValidTag(text, 7) ? OutputLine.OutputChannel.TaggedInfo : OutputLine.OutputChannel.Info,
                    text[7..]);
            }
            else if (text.StartsWith("warning: "))
            {
                line = new OutputLine(OutputLine.OutputChannel.Warning, text[9..]);
            }
            else if (text.StartsWith("error: "))
            {
                line = new OutputLine(OutputLine.OutputChannel.Error, text[7..]);
            }
            else
            {
                line = new OutputLine(OutputLine.OutputChannel.Unknown, text);
            }

            return handleOutput(line) &&
                   (line.Channel != OutputLine.OutputChannel.Error ||
                    options.HasFlag(PerforceProvider.CommandOptions.NoFailOnErrors)) &&
                   line.Channel != OutputLine.OutputChannel.Unknown;
        }

        return true;
    }
}