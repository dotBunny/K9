﻿using System;

namespace K9.Utils
{
    public static class StringUtil
    {
        public static string MarkedSubstring(this string Source, string Marker, string Terminator)
        {
            int startIndex = Source.IndexOf(Marker, StringComparison.Ordinal);

            // We didnt find anything
            if (startIndex < 0)
            {
                return string.Empty;
            }

            int markerLength = Marker.Length;
            int start = startIndex + markerLength;

            return Source.Substring(start,
                Source.IndexOf(Terminator, startIndex + markerLength, StringComparison.Ordinal) - start);
        }
    }
}