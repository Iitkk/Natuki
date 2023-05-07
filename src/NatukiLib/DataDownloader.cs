namespace NatukiLib
{
    using AngleSharp.Html.Parser;
    using log4net;
    using NatukiLib.Analyzers;
    using NatukiLib.Utils;

    public sealed class DataDownloader
    {
        #region 静的関数

        public static string GetPartialAnalysisUrl(string ncode, DateTime dateTime)
            => $@"https://kasasagi.hinaproject.com/access/chapter/ncode/{ncode}/?date={dateTime:yyyy-MM-dd}";

        public static string GetNovelInfoUrl(string ncode, bool isR18)
            => $@"https://api.syosetu.com/novel{(isR18 ? "18" : "")}api/api/?ncode={ncode}&opt=weekly";

        #endregion

        #region コンストラクタ
        public DataDownloader(string sourceCacheDirectoryPath, int? downloadInterval = null, int? downloadTimeOut = null, string? httpClientUserAgent = null)
        {
            SourceCacheDirectoryPath = sourceCacheDirectoryPath;
            DownloadInterval = downloadInterval ?? CommonUtil.DefaultDownloadInterval;
            DownloadTimeOut = downloadTimeOut ?? CommonUtil.DefaultDownloadTimeOut;
            HttpClientUserAgent = httpClientUserAgent ?? CommonUtil.DefaultHttpClientUserAgent;
        }

        #endregion

        #region 共通

        private readonly ILog Logger = CommonUtil.Logger;

        public string HttpClientUserAgent { get; init; }

        public string SourceCacheDirectoryPath { get; init; }

        public int DownloadInterval { get; init; }

        public int DownloadTimeOut { get; init; }

        #endregion

        #region 情報

        public async Task DownloadNovelInfo(string ncode, bool enableR18)
        {
            using var httpClientHelper = new HttpClientHelper(HttpClientUserAgent, DownloadTimeOut);
            async Task<bool> TryDownload(HttpClientHelper httpClientHelper, bool isR18)
            {
                var source = await GetSource(httpClientHelper, GetNovelInfoUrl(ncode, isR18));
                if (source == string.Empty)
                    Logger.Error($"ファイルがダウンロードできません。コード：{ncode}");
                else
                {
                    for (var i = 0; i < CommonUtil.TryCount; i++)
                        try
                        {
                            var yamlInfoAnalyzer = new YamlInfoAnalyzer(source).First;
                            if (yamlInfoAnalyzer is not null)
                            {
                                var filePath = CommonUtil.GetCachedNovelInfoSourceFilePath(SourceCacheDirectoryPath, ncode);
                                if (PathUtil.CreateDirectory(filePath) is not null)
                                {
                                    File.WriteAllText(filePath, source);
                                    var newFilePath = CommonUtil.GetCachedNovelInfoSourceFilePath(SourceCacheDirectoryPath, ncode, yamlInfoAnalyzer.UpdatedDateTime);
                                    if (!File.Exists(newFilePath)) File.Copy(filePath, newFilePath);
                                    return true;
                                }
                            }
                        }
                        catch (InvalidCastException e)
                        {
                            Logger.Error(e.Message + Environment.NewLine + source);
                            if (i == CommonUtil.TryCount - 1)
                                throw;
                        }
                }
                return false;
            }

            if (!await TryDownload(httpClientHelper, false) && enableR18)
                await TryDownload(httpClientHelper, true);
        }

        #endregion

        #region 部分別

        public async Task DownloadPartialAnalysisData(string ncode, DateTime[] dates, Action<object>? updateProgressAction = null)
        {
            var sortedDates = dates.OrderBy(x => x.Ticks).ToArray();
            bool IsInvalidSource(string source)
            {
                var parser = new HtmlParser();
                var doc = parser.ParseDocument(source);
                if (doc is null)
                {
                    Logger.Error("ソースが解析できません。");
                    return true;
                }

                var title = doc.Title;
                return title is not null && (title.EndsWith("Bad Request") || title.EndsWith("アクセス解析 準備中"));
            }

            bool WritesFile(string source) => DataConverter.TryParsePartialDataSource(source, out _, out _, out var maxNumber, out _) && maxNumber.Value > 0;

            using var httpClientHelper = new HttpClientHelper(HttpClientUserAgent, DownloadTimeOut);
            var latestAvailableDate = DateTime.Today.AddDays(-1);
            foreach (var dateTime in sortedDates)
                if (dateTime <= latestAvailableDate)
                {
                    var filePath = CommonUtil.GetCachedPartialAnalysisSourceFilePath(SourceCacheDirectoryPath, ncode, dateTime);
                    if (!File.Exists(filePath))
                    {
                        var directoryPath = Path.GetDirectoryName(filePath);
                        if (directoryPath is not null && !Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
                        if (IsInvalidSource(
                            await GetSource(
                                httpClientHelper, GetPartialAnalysisUrl(ncode, dateTime), filePath, writesFileFunc: dateTime < latestAvailableDate ? default : WritesFile,
                                getErrorLogTextFunc: exceptions => $"ファイルがダウンロードできません。コード：{ncode}　日付：{dateTime:yyyy-MM-dd}　エラー：{exceptions.Message}")))
                            Logger.Error($"ダウンロードしたファイルが無効です。コード：{ncode}　日付：{dateTime:yyyy-MM-dd}");
                    }

                    updateProgressAction?.Invoke(dateTime);
                }
        }

        public async Task<string> GetSource(
            HttpClientHelper httpClientHelper, string url, string? cachingFilePath = null, bool disableCache = false, Func<string, bool>? writesFileFunc = null, Func<Exception, string>? getErrorLogTextFunc = null)
        {
            for (var i = 0; i < CommonUtil.TryCount; i++)
                try
                {
                    var source = await httpClientHelper.GetAsync(url, DownloadTimeOut * Math.Min(10, (i / 3 + 1)));
                    Thread.Sleep(DownloadInterval);
                    if (cachingFilePath is not null && !disableCache)
                    {
                        PathUtil.CreateDirectory(cachingFilePath);
                        if (writesFileFunc is null || writesFileFunc(source))
                            File.WriteAllText(cachingFilePath, source);
                    }
                    return source;
                }
                catch (Exception e)
                {
                    if (i == CommonUtil.TryCount - 1)
                    {
                        Logger.Error(getErrorLogTextFunc is null ? $"ファイルがダウンロードできません。URL：{url}　エラー：{e.Message}" : getErrorLogTextFunc(e));
                        throw;
                    }
                }

            throw new NotSupportedException();
        }

        #endregion
    }
}
