using System.Collections.Generic;
using System.Linq;

namespace DevOps.Build.Tools.LocalNuGetSource.BeforeBuild
{
    internal static class PackageDictionaryHelper
    {
        public static void Add(this Dictionary<string, string> dictionary, string line)
            => dictionary.TryAdd(
                line.Parse("Include"),
                line.Parse("Version"));

        public static string GetFileName(this KeyValuePair<string, string> pair)
            => $"{pair.Key}.{pair.Value}.nupkg";

        private static string Parse(this string line, string token)
            => line.Split($"{token}=\"").Last().Split("\"").First();
    }
}
