namespace NatukiLib
{
    using NatukiLib.Analyzers;

    public class TaskHelper
    {
        #region コンストラクタ

        public TaskHelper(
            string ncode, DateTime[]? dates = null, string? sourceCacheDirectoryPath = null, string? outputDirectoryPath = null,
            Action<object>? updateProgressAction = null, bool enableR18 = false, int? downloadInterval = null, int? cancelingDownloadDuration = null, string? httpClientUserAgent = null)
        {
            Ncode = ncode;
            Dates = dates;
            SourceCacheDirectoryPath = sourceCacheDirectoryPath ?? CommonUtil.DefaultSourceDirectoryPath;
            OutputDirectoryPath = outputDirectoryPath ?? CommonUtil.DefaultDataDirectoryPath;
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

        public string SourceCacheDirectoryPath { get; init; }

        public string OutputDirectoryPath { get; init; }

        #endregion

        #endregion

        #region 情報

        public string Ncode { get; init; }

        public DateTime[]? Dates { get; init; }

        public DateTime[] TargetDates => GetTargetDates();

        private DateTime[] GetTargetDates()
        {
            var dates = Dates;
            var endDate = dates?.LastOrDefault() ?? DateTime.Today.AddDays(-1);
            var path = OutputDirectoryPath;
            var filePath = CommonUtil.GetInfoDataTextFilePath(path, Ncode);
            if (File.Exists(filePath))
            {
                var infoDataAnalyer = new InfoDataAnalyer(Ncode, path);
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

        public async Task Run(bool isOnlyConversion)
        {
            var dataDownloader = new DataDownloader(SourceCacheDirectoryPath, DownloadInterval, CancelingDownloadDuration, HttpClientUserAgent);

            #region 情報

            if (!isOnlyConversion)
                await Task.Run(() => dataDownloader.DownloadNovelInfo(Ncode, EnableR18));
            var dataConverter = new DataConverter(SourceCacheDirectoryPath, OutputDirectoryPath);
            await Task.Run(() => dataConverter.UpdateInfoData(Ncode));
            var workDataAnalyzer = new WorkDataAnalyzer(Ncode, OutputDirectoryPath);

            #endregion

            #region アクセス

            if (workDataAnalyzer.HasInfo && int.Parse(workDataAnalyzer.GetValue("novel_type")) != 2 && int.Parse(workDataAnalyzer.GetValue("general_all_no")) > 1)
            {
                var targetDates = TargetDates;
                if (!isOnlyConversion && targetDates.Length > 0)
                    await Task.Run(() => dataDownloader.DownloadPartialAnalysisData(Ncode, targetDates, UpdateProgressAction));
                await Task.Run(() => dataConverter.UpdateAccessData(Ncode, targetDates));
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
