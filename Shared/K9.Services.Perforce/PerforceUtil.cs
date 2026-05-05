// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

namespace K9.Services.Perforce;

public static class PerforceUtil
{

    static bool TryStripPrefix(string text, string prefix, out string body)
    {
        if (text.Length == prefix.Length && text.StartsWith(prefix))
        {
            body = string.Empty;
            return true;
        }

        if (text.Length > prefix.Length && text.StartsWith(prefix) && text[prefix.Length] == ' ')
        {
            body = text[(prefix.Length + 1)..];
            return true;
        }

        body = string.Empty;
        return false;
    }

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
            if (TryStripPrefix(text, "text:", out string textBody))
            {
                line = new OutputLine(OutputLine.OutputChannel.Text, textBody);
            }
            else if (TryStripPrefix(text, "info:", out string infoBody))
            {
                line = new OutputLine(OutputLine.OutputChannel.Info, infoBody);
            }
            else if (TryStripPrefix(text, "info1:", out string info1Body))
            {
                line = new OutputLine(
                    IsValidTag(text, 7) ? OutputLine.OutputChannel.TaggedInfo : OutputLine.OutputChannel.Info,
                    info1Body);
            }
            else if (TryStripPrefix(text, "warning:", out string warningBody))
            {
                line = new OutputLine(OutputLine.OutputChannel.Warning, warningBody);
            }
            else if (TryStripPrefix(text, "error:", out string errorBody))
            {
                line = new OutputLine(OutputLine.OutputChannel.Error, errorBody);
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