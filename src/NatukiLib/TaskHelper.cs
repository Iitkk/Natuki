namespace NatukiLib
{
    using NatukiLib.Analyzers;
    using NatukiLib.Utils;

    public class TaskHelper
    {
        #region コンストラクタ

        public TaskHelper(
            string ncode, DateTime[]? dates = null, string[]? sourceDirectoryPaths = null, string? dataDirectoryPath = null, string? categoryDataDirectoryPath = null,
            Action<object>? updateProgressAction = null, bool enableR18 = false, int? downloadInterval = null, int? cancelingDownloadDuration = null, string? httpClientUserAgent = null)
        {
            Ncode = ncode;
            Dates = dates;
            SourceDirectoryPaths = sourceDirectoryPaths ?? new[] { CommonUtil.DefaultSourceDirectoryPath };
            DataDirectoryPath = dataDirectoryPath ?? CommonUtil.DefaultDataDirectoryPath;
            CategoryDataDirectoryPath = categoryDataDirectoryPath;
            UpdateProgressAction = updateProgressAction;
            EnableR18 = enableR18;
            DownloadInterval = downloadInterval;
            CancelingDownloadDuration = cancelingDownloadDuration;
            HttpClientUserAgent = httpClientUserAgent;
        }

        #endregion

        #region 設定

        public bool EnableR18 { get; init; }

        public int? DownloadInterval { get; init; }

        public int? CancelingDownloadDuration { get; init; }

        public string? HttpClientUserAgent { get; init; }

        #region パス

        public string[] SourceDirectoryPaths { get; init; }

        public string DataDirectoryPath { get; init; }

        public string? CategoryDataDirectoryPath { get; set; }

        #endregion

        #endregion

        #region 情報

        public string Ncode { get; init; }

        public DateTime[]? Dates { get; init; }

        private DateTime[] GetTargetDates(string categoryDataDirectoryPath)
        {
            var dates = Dates;
            var endDate = dates?.LastOrDefault() ?? DateTime.Today.AddDays(-1);
            var path = categoryDataDirectoryPath;
            var filePath = CommonUtil.GetInfoDataTextFilePath(path, Ncode);
            if (File.Exists(filePath))
            {
                var infoDataAnalyer = new InfoDataAnalyzer(Ncode, path);
                var startDate = infoDataAnalyer.FirstUploadDateTime.Date;
                if (dates is null)
                {
                    var currentDate = startDate;
                    var dateList = new List<DateTime>();
                    while (currentDate <= endDate)
                    {
                        dateList.Add(currentDate);
                        currentDate = currentDate.AddDays(1);
                    }
                    return dateList.ToArray();
                }
                else
                    return dates.Where(x => x >= startDate).ToArray();
            }
            else
                return new[] { endDate };
        }

        #endregion

        #region 処理

        public async Task Run() => await Run(false);

        private string GetNewCategoryDataDirectory()
        {
            static string? GetCategoryDataDirectoryPath(string ncode, string dataPath)
            {
                foreach (var directory in Directory.GetDirectories(dataPath))
                    if (Path.GetFileName(directory) == ncode)
                        return Path.GetDirectoryName(directory);
                    else
                    {
                        var subDirectoryResult = GetCategoryDataDirectoryPath(ncode, directory);
                        if (subDirectoryResult != null) return subDirectoryResult;
                    }

                return null;
            }

            var ncode = Ncode;
            var currentCategoryDataDirectoryPath = GetCategoryDataDirectoryPath(ncode, DataDirectoryPath);
            if (currentCategoryDataDirectoryPath is not null) return currentCategoryDataDirectoryPath;

            foreach (var sourceDirectoryPath in SourceDirectoryPaths)
            {
                var filePath = CommonUtil.GetCachedNovelInfoSourceFilePath(sourceDirectoryPath, ncode);
                if (File.Exists(filePath))
                {
                    var yamlInfoAnalyzer = new YamlInfoAnalyzer(File.ReadAllText(filePath));
                    if (yamlInfoAnalyzer.IsSingleData)
                    {
                        var yamlWorkInfoAnalyzer = yamlInfoAnalyzer.GetYamlWorkInfoAnalyzer(ncode);
                        var firstUploadDateTime = yamlWorkInfoAnalyzer.FirstUploadDateTime;
                        return Path.Combine(DataDirectoryPath, PathUtil.GetYearAndMonthDirectory(firstUploadDateTime));
                    }
                }
            }

            throw new NotSupportedException($"{ncode}は存在しません。");
        }

        public async Task Run(bool isOnlyConversion)
        {
            var dataDownloader = new DataDownloader(SourceDirectoryPaths, DownloadInterval, CancelingDownloadDuration, HttpClientUserAgent);

            #region 情報

            if (!isOnlyConversion)
                await Task.Run(() => dataDownloader.DownloadNovelInfo(Ncode, EnableR18));

            string categoryDataDirectoryPath;
            if (CategoryDataDirectoryPath is null)
            {
                CategoryDataDirectoryPath = categoryDataDirectoryPath = GetNewCategoryDataDirectory();
            }
            else
                categoryDataDirectoryPath = CategoryDataDirectoryPath;

            var dataConverter = new DataConverter(SourceDirectoryPaths, categoryDataDirectoryPath);
            await Task.Run(() => dataConverter.UpdateInfoData(Ncode));
            var workDataAnalyzer = new WorkDataAnalyzer(Ncode, categoryDataDirectoryPath);

            #endregion

            #region アクセス

            if (workDataAnalyzer.HasInfo && int.Parse(workDataAnalyzer.GetValue("novel_type")) != 2 && int.Parse(workDataAnalyzer.GetValue("general_all_no")) > 1)
            {
                var targetDates = GetTargetDates(categoryDataDirectoryPath);
                if (!isOnlyConversion && targetDates.Length > 0)
                    await Task.Run(() => dataDownloader.DownloadPartialAnalysisData(Ncode, targetDates, UpdateProgressAction));
                await Task.Run(() => dataConverter.UpdateAccessData(Ncode, targetDates, UpdateProgressAction));
            }

            #endregion

            CommonUtil.Logger.Info("処理が終了しました。");
        }

        #endregion

        #region プログレス

        public Action<object>? UpdateProgressAction { get; init; }

        #endregion

    }
}
