namespace NatukiLib.Analyzers
{
    using YamlDotNet.RepresentationModel;

    public class YamlInfoAnalyzer
    {
        public YamlInfoAnalyzer(string source)
        {
            using var reader = new StringReader(source);
            var ys = new YamlStream();
            ys.Load(reader);

            var yamlWorkInfoAnalyzerMap = new Dictionary<string, YamlWorkInfoAnalyzer>(StringComparer.OrdinalIgnoreCase);
            foreach (var yamlDocument in ys)
                foreach (YamlMappingNode baseNode in ((YamlSequenceNode)yamlDocument.RootNode).Children)
                {
                    var infoMap = new Dictionary<string, string>();
                    foreach (var keyAndValue in baseNode.Children)
                        infoMap.Add(keyAndValue.Key.ToString() ?? string.Empty, keyAndValue.Value.ToString() ?? string.Empty);

                    if (infoMap.TryGetValue("ncode", out var ncode))
                        yamlWorkInfoAnalyzerMap.Add(ncode.ToLower(), new YamlWorkInfoAnalyzer(infoMap));
                    else
                        AllCount = int.Parse(infoMap["allcount"]);
                }
            YamlWorkInfoAnalyzerMap = yamlWorkInfoAnalyzerMap;
        }

        public int AllCount { get; }

        public bool IsSingleData => AllCount == 1;

        public Dictionary<string, YamlWorkInfoAnalyzer> YamlWorkInfoAnalyzerMap { get; }

        public YamlWorkInfoAnalyzer? First => YamlWorkInfoAnalyzerMap.Values.FirstOrDefault();

        public YamlWorkInfoAnalyzer GetYamlWorkInfoAnalyzer(string ncode) => YamlWorkInfoAnalyzerMap[ncode];
    }
}
