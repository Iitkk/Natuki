namespace NatukiLib.Analyzers
{
    using System.Text;
    using YamlDotNet.Core;
    using YamlDotNet.RepresentationModel;

    public class YamlInfoAnalyzer
    {
        private static void Parse(
            string source, out List<YamlWorkInfoAnalyzer> yamlWorkInfoAnalyzerList, out Dictionary<string, YamlWorkInfoAnalyzer> yamlWorkInfoAnalyzerMap, out int allCount)
        {
            const int MaxErrorCount = 100;
            var previousErrorIndex = -1;
            var _yamlWorkInfoAnalyzerList = new List<YamlWorkInfoAnalyzer>();
            var _yamlWorkInfoAnalyzerMap = new Dictionary<string, YamlWorkInfoAnalyzer>(StringComparer.OrdinalIgnoreCase);
            var checkingErrorCount = 0;
            var _allCount = 0;
            while (true)
            {
                using var reader = new StringReader(source);
                try
                {
                    var ys = new YamlStream();
                    ys.Load(reader);

                    foreach (var yamlDocument in ys)
                        foreach (YamlMappingNode baseNode in ((YamlSequenceNode)yamlDocument.RootNode).Children)
                        {
                            var infoMap = new Dictionary<string, string>();
                            foreach (var keyAndValue in baseNode.Children)
                                infoMap.Add(keyAndValue.Key.ToString() ?? string.Empty, keyAndValue.Value.ToString() ?? string.Empty);

                            if (infoMap.TryGetValue("ncode", out var ncode))
                            {
                                var yamlWorkInfoAnalyzer = new YamlWorkInfoAnalyzer(infoMap);
                                _yamlWorkInfoAnalyzerList.Add(yamlWorkInfoAnalyzer);
                                _yamlWorkInfoAnalyzerMap.Add(ncode.ToLower(), yamlWorkInfoAnalyzer);
                            }
                            else
                                _allCount = int.Parse(infoMap["allcount"]);
                        }
                    break;
                }
                catch (SemanticErrorException e)
                {
                    #region 余分な半角空白を全角空白に変換

                    var start = e.Start;
                    var end = e.End;
                    var contentStartIndex = source.LastIndexOf(':', start.Index);
                    var nextLineIndex = source.IndexOf('\n', contentStartIndex + 1);
                    if (nextLineIndex != -1 && (source.IndexOf('|', contentStartIndex + 1) < nextLineIndex || source.IndexOf('>', contentStartIndex + 1) < nextLineIndex))
                    {
                        var actualContentStartIndex = nextLineIndex + 1;
                        var contentEndIndex = source.IndexOf('\n', end.Index);
                        var content = source.Substring(actualContentStartIndex, contentEndIndex - actualContentStartIndex);
                        var lines = content.Split('\n');
                        var minBlankIndex = int.MaxValue;
                        var startBlankLengthAndIndexList = new List<(int StartBlankLength, int Index)>();
                        {
                            var currentIndex = 0;
                            while (currentIndex < content.Length)
                            {
                                var startCurrentIndex = currentIndex;
                                while (content[currentIndex] == ' ')
                                    currentIndex++;

                                var startBlankLength = currentIndex - startCurrentIndex;
                                if (startBlankLength > 0)
                                {
                                    startBlankLengthAndIndexList.Add((startBlankLength, currentIndex));
                                    if (startBlankLength < minBlankIndex) minBlankIndex = currentIndex - startCurrentIndex;
                                }

                                currentIndex = content.IndexOf('\n', currentIndex);
                                if (currentIndex == -1)
                                    break;
                                else
                                    currentIndex++;
                            }
                        }

                        var newSourceStringBuilder = new StringBuilder();
                        {
                            var currentIndex = 0;
                            foreach (var startBlankLengthAndIndex in startBlankLengthAndIndexList)
                                if (startBlankLengthAndIndex.StartBlankLength > minBlankIndex)
                                {
                                    var nextStartIndex = startBlankLengthAndIndex.Index + actualContentStartIndex;
                                    var diff = startBlankLengthAndIndex.StartBlankLength - minBlankIndex;
                                    newSourceStringBuilder.Append(source.AsSpan(currentIndex, nextStartIndex - currentIndex - diff));
                                    newSourceStringBuilder.Append(new string('　', diff));
                                    currentIndex = nextStartIndex;
                                }
                            if (currentIndex < source.Length)
                                newSourceStringBuilder.Append(source.AsSpan(currentIndex));
                        }

                        source = newSourceStringBuilder.ToString();
                        #endregion
                    }
                    else
                        throw;

                    if (previousErrorIndex != start.Index)
                    {
                        checkingErrorCount = 0;
                        previousErrorIndex = start.Index;
                    }
                    else if (++checkingErrorCount == MaxErrorCount)
                        throw;
                }
            }

            yamlWorkInfoAnalyzerList = _yamlWorkInfoAnalyzerList;
            yamlWorkInfoAnalyzerMap = _yamlWorkInfoAnalyzerMap;
            allCount = _allCount;
        }

        public YamlInfoAnalyzer(string source)
        {
            Parse(source, out var yamlWorkInfoAnalyzerList, out var yamlWorkInfoAnalyzerMap, out var allCount);
            YamlWorkInfoAnalyzerList = yamlWorkInfoAnalyzerList;
            YamlWorkInfoAnalyzerMap = yamlWorkInfoAnalyzerMap;
            AllCount = allCount;
        }

        public int AllCount { get; }

        public bool IsSingleData => AllCount == 1;

        #region YamlWorkInfoAnalyzer

        public List<YamlWorkInfoAnalyzer> YamlWorkInfoAnalyzerList { get; }

        public Dictionary<string, YamlWorkInfoAnalyzer> YamlWorkInfoAnalyzerMap { get; }

        public YamlWorkInfoAnalyzer? First => YamlWorkInfoAnalyzerList.FirstOrDefault();

        public YamlWorkInfoAnalyzer GetYamlWorkInfoAnalyzer(string ncode) => YamlWorkInfoAnalyzerMap[ncode];

        #endregion
    }
}
