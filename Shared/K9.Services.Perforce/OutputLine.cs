// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

namespace K9.Services.Perforce;

public class OutputLine(OutputLine.OutputChannel inChannel, string inText)
{
    public enum OutputChannel
    {
        Unknown,
        Text,
        Info,
        TaggedInfo,
        Warning,
        Error,
        Exit
    }

    public readonly OutputChannel Channel = inChannel;
    public readonly string Text = inText;

    public override string ToString()
    {
        return $"{Channel}: {Text}";
    }
}