using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class StringExtensions
    {
        public static int IndexOfOccurrence(this string text, char character, int occurrenceNumber)
        {
            int index = 0;
            for (int i = 0; i < occurrenceNumber; i++)
            {
                if (index < 0)
                    break;
                index = text.IndexOf(character, index + (i > 0 ? 1 : 0));
            }
            return index;
        }

        public static int IndexOfOccurrence(this string sourceText, string value, int occurrenceNumber)
        {
            int index = 0;
            for (int i = 0; i < occurrenceNumber; i++)
            {
                if (index < 0)
                    break;
                index = sourceText.IndexOf(value, index + (i > 0 ? 1 : 0));
            }
            return index;
        }

        public static string Remove(this string srcStr, string value)
        {
            return Remove(srcStr, value, StringComparison.CurrentCulture);
        }

        public static string Remove(this string srcStr, string value, StringComparison comparisonType)
        {
            string finalStr = srcStr;
            while (finalStr.IndexOf(value, comparisonType) >= 0)
            {
                finalStr = finalStr.Remove(finalStr.IndexOf(value, comparisonType), value.Length);
            }
            return finalStr;
        }

        public static string Remove(this string srcStr, params string[] values)
        {
            return Remove(srcStr, StringComparison.CurrentCulture, values);
        }

        public static string Remove(this string srcStr, StringComparison comparisonType, params string[] values)
        {
            string finalStr = srcStr;
            for (int i = 0; i < values.Length; i++)
            {
                finalStr = finalStr.Remove(values[i], comparisonType);
            }
            return finalStr;
        }

        public static string RemoveFirst(this string srcStr, string value)
        {
            return RemoveFirst(srcStr, value, StringComparison.CurrentCulture);
        }

        public static string RemoveFirst(this string srcStr, string value, StringComparison comparisonType)
        {
            if (srcStr.IndexOf(value, comparisonType) >= 0)
            {
                return srcStr.Remove(srcStr.IndexOf(value), value.Length);
            }
            return srcStr;
        }

        public static string RemoveChars(this string srcStr, params char[] values)
        {
            string finalStr = srcStr;
            for (int i = 0; i < values.Length; i++)
            {
                while (finalStr.IndexOf(values[i]) >= 0)
                {
                    finalStr = finalStr.Remove(finalStr.IndexOf(values[i]), 1);
                }
                //finalStr.Replace(values[i].ToString(), string.Empty);
            }
            return finalStr;
        }

        public static string FlatConcat(this IEnumerable<string> textArray, string separator)
        {
            return textArray.Aggregate((a, b) => a + separator + b);
        }

        public static string PadRight(this object obj, int totalWidth)
        {
            return (obj.ToString()).PadRight(totalWidth);
        }
    }
}
