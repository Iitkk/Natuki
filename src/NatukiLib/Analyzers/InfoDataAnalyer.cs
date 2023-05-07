namespace NatukiLib.Analyzers
{
    using NatukiLib.Utils;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class InfoDataAnalyer
    {
        public InfoDataAnalyer(string ncode, string dataDirectoryPath)
        {
            Ncode = ncode;
            DataDirectoryPath = dataDirectoryPath;
            infoMap = null;
        }

        public string Ncode { get; }

        public string DataDirectoryPath { get; }



        private Dictionary<string, string>? infoMap;
        public Dictionary<string, string> InfoMap { get => infoMap ??= DataUtil.CreateValueMap(CommonUtil.GetInfoDataTextFilePath(DataDirectoryPath, Ncode)); }

        public DateTime GetDateTime(string key) => DateTime.Parse(InfoMap[key]);


        public DateTime FirstUploadDateTime => GetDateTime("general_firstup");

        public DateTime UpdatedDateTime => GetDateTime("updated_at");
    }
}
