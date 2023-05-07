namespace NatukiLib.Utils
{
    using System.Net;

    public enum ViewDataType
    {
        UniqueAccess,
        RetentionRate,
        AbandonmentRate,
        RetentionRateOnPreviousStory,
        AbandonmentRateOnPreviousStory
    }

    public static class DataUtil
    {
        public static readonly string NewLineEscapeText = "&#xa;";

        private static string ReplaceText(string target) => target.Replace("\n", NewLineEscapeText).Replace(Environment.NewLine, NewLineEscapeText);

        private static string ReverseText(string target) => WebUtility.HtmlDecode(target.Replace(NewLineEscapeText, "\n"))!.TrimStart('|', '>');

        public static string[] GetNcodesInDataDirectory(string? outputDirectoryPath)
        {
            if (outputDirectoryPath is not null)
            {
                var di = new DirectoryInfo(outputDirectoryPath);
                if (di.Exists)
                    return di.GetDirectories().Where(x => NarouDefinitionUtil.IsNcode(x.Name)).Select(x => x.Name).ToArray();
            }

            return Array.Empty<string>();
        }

        public static Dictionary<string, string> CreateValueMap(string filePath)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(filePath))
                foreach (var line in File.ReadLines(filePath))
                {
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex != -1)
                        map.Add(line.Substring(0, colonIndex), colonIndex < line.Length - 1 ? ReverseText(line.Substring(colonIndex + 1)) : string.Empty);
                }
            return map;
        }

        public static IEnumerable<string> GetLines(Dictionary<string, string> map)
        {
            foreach (var line in map.OrderBy(x => x.Key).Select(x => x.Key + ":" + ReplaceText(x.Value)))
                yield return line;
        }
    }
}
