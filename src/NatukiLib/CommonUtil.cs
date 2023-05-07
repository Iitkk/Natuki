[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace NatukiLib
{
    using log4net;
    using log4net.Repository.Hierarchy;
    using System.Linq;
    using System.Reflection;

    public static class CommonUtil
    {
        private static readonly ILog logger = LogManager.GetLogger((Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Name);

        public static ILog Logger => logger;

        public static bool HasError() => ((Hierarchy)LogManager.GetRepository()).GetAppenders().OfType<ErrorFlagAppender>().Any(f => f.ErrorOccurred);

        public static int TryCount { get; set; } = 20;

        #region パス

        #region キャッシュデータ

        /// <summary>
        /// 部分分析ソースのキャッシュ用のパスを取得する。
        /// </summary>
        public static string GetCachedPartialAnalysisSourceFilePath(string sourceCacheDirectoryPath, string ncode, DateTime dateTime)
             => Path.Combine(sourceCacheDirectoryPath, ncode, "Partial", $@"{ncode}.partial.{dateTime.ToString("yyyyMMdd")}.html");

        public static string GetCachedNovelInfoSourceFilePath(string sourceCacheDirectoryPath, string ncode, DateTime? dateTime = null)
             => Path.Combine(sourceCacheDirectoryPath, ncode, "Info", $@"{ncode}.info{(dateTime.HasValue ? "." + dateTime.Value.ToString("yyyyMMddhhmmss") : string.Empty)}.yml");

        public static string GetCachedFilePath(string sourceCacheDirectoryPath, string filePath, bool createsDirectory = false)
             => Combine(sourceCacheDirectoryPath, filePath, createsDirectory);

        public static string Combine(string directoryPath, string filePath, bool createsDirectory)
        {
            var path = Path.Combine(directoryPath, filePath);
            var targetDirectoryPath = Path.GetDirectoryName(path);
            if (createsDirectory && targetDirectoryPath is not null && !Directory.Exists(targetDirectoryPath))
                Directory.CreateDirectory(targetDirectoryPath);
            return path;
        }

        #endregion

        #region CSV

        public static string GetAccessDataCsvFilePath(string dataDirectoryPath, string ncode, bool createsDirectory = false)
            => Combine(Path.Combine(dataDirectoryPath, ncode), ncode + ".access.csv", createsDirectory);

        #endregion

        #region Text

        public static string GetInfoDataTextFilePath(string dataDirectoryPath, string ncode, bool createsDirectory = false)
            => Combine(Path.Combine(dataDirectoryPath, ncode), ncode + ".info.txt", createsDirectory);

        #endregion

        #region 既定

        public static readonly string DefaultSourceDirectoryPath = "Source";

        public static readonly string DefaultDataDirectoryPath = "Data";

        #endregion

        #endregion

        #region 設定

        public static readonly string DefaultDateFormat = "yyyy-MM-dd";

        public static string DateFormat { get; set; } = DefaultDateFormat;

        public static readonly int DefaultDownloadInterval = 1000;

        public static readonly int DefaultDownloadTimeOut = 5000;

        public static readonly string DefaultHttpClientUserAgent = "AnalysisTool";

        #endregion
    }
}
