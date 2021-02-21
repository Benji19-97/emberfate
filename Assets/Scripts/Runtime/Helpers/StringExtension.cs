using System.Collections.Generic;

namespace Runtime.Helpers
{
    public static class StringExtension
    {
        public static IEnumerable<int> AllIndexesOf(this string str, string searchString)
        {
            int minIndex = str.IndexOf(searchString);
            while (minIndex != -1)
            {
                yield return minIndex;
                minIndex = str.IndexOf(searchString, minIndex + searchString.Length);
            }
        }
    }
}