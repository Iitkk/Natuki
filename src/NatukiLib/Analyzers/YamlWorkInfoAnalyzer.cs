namespace NatukiLib.Analyzers
{
    public class YamlWorkInfoAnalyzer
    {
        public YamlWorkInfoAnalyzer(Dictionary<string, string> infoMap)
        {
            InfoMap = infoMap;
        }

        public Dictionary<string, string> InfoMap { get; }

        public DateTime GetDateTime(string key) => DateTime.Parse(InfoMap[key]);

        public DateTime FirstUploadDateTime => GetDateTime("general_firstup");

        public DateTime LastUploadDateTime => GetDateTime("general_lastup");

        public DateTime UpdatedDateTime => GetDateTime("updated_at");
    }
}
