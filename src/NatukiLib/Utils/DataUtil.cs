namespace NatukiLib.Utils
{
    using System.Net;

    public enum ViewDataType
    {
        SubtotalUniqueAccess,
        RetentionRate,
        AbandonmentRate,
        RetentionRateToNextStory,
        AbandonmentRateToNextStory,
        UniqueAccessByDate
    }

    public static class DataUtil
    {
        public static readonly string NewLineEscapeText = "&#xa;";

        private static string ReplaceText(string target) => target.Replace("\n", NewLineEscapeText).Replace(Environment.NewLine, NewLineEscapeText);

        private static string ReverseText(string target) => WebUtility.HtmlDecode(target.Replace(NewLineEscapeText, "\n"))!.TrimStart('|', '>');

        public static DirectoryInfo[] GetNcodeDirectoryInfos(string? dataDirectoryPath, bool? isTimeSortedNcode = null)
        {
            if (dataDirectoryPath is not null)
            {
                var directoryInfo = new DirectoryInfo(dataDirectoryPath);
                if (directoryInfo.Exists)
                {
                    var directoryInfoList = new List<DirectoryInfo>();
                    var directoryNameSet = new HashSet<string>();

                    void Add(DirectoryInfo directoryInfo)
                    {
                        foreach (var subDirectoryInfo in directoryInfo.GetDirectories())
                            if (NarouDefinitionUtil.IsNcode(subDirectoryInfo.Name))
                            {
                                if (directoryNameSet.Add(subDirectoryInfo.Name))
                                    directoryInfoList.Add(subDirectoryInfo);
                            }
                            else
                                Add(subDirectoryInfo);
                    }

                    Add(directoryInfo);

                    return isTimeSortedNcode.HasValue ? directoryInfoList.OrderBy(x => NarouDefinitionUtil.GetIndex(x.Name, !isTimeSortedNcode.Value)).ToArray() : directoryInfoList.ToArray();
                }
            }

            return Array.Empty<DirectoryInfo>();
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

        public static string TrimYamlContent(string target) => target.TrimStart('|', '>').Trim();
    }
}
