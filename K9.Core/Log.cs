using System;

namespace K9
{
    public static class Log
    {
        private const string DateStampFormat = "yyyy-MM-dd HH:mm:ss";
        private const int FixedCategoryLength = 12;
        private const string HeavyDivider = "==========================================";
        private const string LightDivider = "------------------------------------------";

        public static void WriteLine(string output, string category = "DEFAULT")
        {
            if (string.IsNullOrEmpty(output)) return;

            Console.WriteLine(
                $"[{DateTime.Now.ToString(DateStampFormat)}] {category.ToUpper().PadLeft(FixedCategoryLength, ' ')} > {output}");
        }

        public static void WriteRaw(string output)
        {
            Console.WriteLine(output);
        }

        public static void Write(string output)
        {
            if (string.IsNullOrEmpty(output)) return;

            Console.Write(output);
        }

        public static void LineFeed()
        {
            Console.WriteLine();
        }

        public static void WriteLightDivider()
        {
            Console.WriteLine(LightDivider);
        }

        public static void WriteHeavyDivider()
        {
            Console.WriteLine(HeavyDivider);
        }
    }
}