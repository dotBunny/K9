namespace K9.Services.Perforce
{
    public class OutputLine
    {
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