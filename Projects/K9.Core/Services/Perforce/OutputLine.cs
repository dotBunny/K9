// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

namespace K9.Services.Perforce
{
    public class OutputLine
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
        
        public readonly OutputChannel Channel;
        public readonly string Text;

        public OutputLine(OutputChannel InChannel, string InText)
        {
            Channel = InChannel;
            Text = InText;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Channel, Text);
        }
    }
}