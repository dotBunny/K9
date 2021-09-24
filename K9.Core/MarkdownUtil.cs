namespace K9
{
    public static class MarkdownUtil
    {
        private static int _orderedListCount = 1;

        public static string Blockquote(string content)
        {
            return $"> {content}";
        }

        public static string Bold(string content)
        {
            return $"**{content}**";
        }

        public static string Code(string code)
        {
            return $"`{code}`";
        }

        public static string H1(string content)
        {
            return $"# {content}";
        }

        public static string H2(string content)
        {
            return $"## {content}";
        }

        public static string H3(string content)
        {
            return $"### {content}";
        }

        public static string HorizontalLine()
        {
            return "---";
        }

        public static string Image(string text, string path)
        {
            return $"![{text}]({path})";
        }

        public static string Italic(string content)
        {
            return $"*{content}*";
        }

        public static string Link(string title, string link)
        {
            return $"[{title}]({link})";
        }

        public static string OrderedList(string item, bool resetCount = false)
        {
            if (resetCount)
            {
                _orderedListCount = 1;
            }

            _orderedListCount++;
            return $"{_orderedListCount - 1}. {item}";
        }

        public static string UnorderedList(string item)
        {
            return $"- {item}";
        }
    }
}