namespace NatukiLib
{
    using AngleSharp.Html.Parser;
    using log4net;
    using NatukiLib.Analyzers;
    using NatukiLib.Utils;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using static NatukiLib.CommonUtil;

    public sealed class DataConverter
    {
        #region 静的関数

        #region 部分別

        #region 取得

        public static string GetPartialAnalysisUrl(string ncode, DateTime dateTime)
            => $@"https://kasasagi.hinaproject.com/access/chapter/ncode/{ncode}/?date={dateTime.ToString("yyyy-MM-dd")}";

        #endregion

        #region 解析

        public static bool TryParsePartialDataSource(
            string source, [NotNullWhen(true)] out string? ncode, [NotNullWhen(true)] out DateTime? dateTime, [NotNullWhen(true)] out int? maxNumber,
            [NotNullWhen(true)] out List<Tuple<DateTime, int, int>>? parsedDataList)
        {
            bool Error(string message, out string? ncode, out DateTime? dateTime, out int? maxNumber, out List<Tuple<DateTime, int, int>>? parsedDataList)
            {
                CommonUtil.Logger.Error("ソースが解析できません。");
                ncode = null;
                dateTime = null;
                maxNumber = null;
                parsedDataList = null;
                return false;
            }

            var parser = new HtmlParser();
            var doc = parser.ParseDocument(source);
            if (doc is null) return Error("ソースが解析できません。", out ncode, out dateTime, out maxNumber, out parsedDataList);

            var cgElement = doc.GetElementById("chapter_graph");
            var href = doc.GetElementsByClassName("novelview_menu").FirstOrDefault()?.FirstElementChild?.FirstElementChild?.GetAttribute("href");
            var dateTimeText = cgElement?.FirstElementChild?.FirstElementChild?.TextContent;

            if (cgElement is null || href is null || dateTimeText is null || !DateTime.TryParseExact(dateTimeText, "■yyyy年MM月dd日のログ", null, DateTimeStyles.None, out var parsedDateTime))
                return Error("ソースが解析できません。", out ncode, out dateTime, out maxNumber, out parsedDataList);

            var startIndex = href.LastIndexOf('/', href.Length - 2);
            if (startIndex == -1)
                return Error("ソースが解析できません。", out ncode, out dateTime, out maxNumber, out parsedDataList);

            ncode = href.Substring(startIndex + 1, href.Length - startIndex - 2);
            dateTime = parsedDateTime;

            var startWord = "第";
            var centerWord = "部分:";
            var endWord = "人";

            var dataList = new List<Tuple<DateTime, int, int>>();
            var _maxNumber = 0;
            foreach (var text in doc.GetElementsByClassName("chapter-graph-list__item").Select(x => x.TextContent))
            {
                var colonIndex = text.IndexOf(centerWord);
                var ninIndex = text.IndexOf(endWord);

                if (int.TryParse(text.AsSpan(startWord.Length + 1, colonIndex - startWord.Length - 1), out var indexValue)
                    && int.TryParse(text.AsSpan(colonIndex + centerWord.Length, ninIndex - (colonIndex + centerWord.Length)), out var ninValue))
                {
                    dataList.Add(Tuple.Create(parsedDateTime, indexValue, ninValue));
                    if (_maxNumber < indexValue) _maxNumber = indexValue;
                }
                else
                    throw new NotSupportedException();
            }

            if (dataList.Count == 0)
                dataList.Add(Tuple.Create(parsedDateTime, 0, 0));

            maxNumber = _maxNumber;
            parsedDataList = dataList;
            return true;
        }

        #endregion

        #endregion

        #endregion

        #region コンストラクタ
        public DataConverter(string sourceCacheDirectoryPath, string outputDirectoryPath)
        {
            SourceCacheDirectoryPath = sourceCacheDirectoryPath;
            OutputDirectoryPath = outputDirectoryPath;
        }

        #endregion

        #region 共通

        private readonly ILog Logger = CommonUtil.Logger;

        #region 情報

        public string SourceCacheDirectoryPath { get; init; }

        public string GetPath(string filePath) => PathUtil.Combine(SourceCacheDirectoryPath, filePath);

        #endregion

        #region 解析

        private bool IsInvalidSource(string source)
        {
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(source);
            if (doc is null)
            {
                Logger.Error("ソースが解析できません。");
                return true;
            }
            return doc.Title?.EndsWith("Bad Request") ?? false;
        }

        #endregion

        #region 出力

        public string OutputDirectoryPath { get; init; }

        public void Update(string ncode, DateTime[] dates)
        {
            if (UpdateInfoData(ncode))
                UpdateAccessData(ncode, dates);
        }

        #endregion

        #endregion

        #region 情報

        public bool UpdateInfoData(string ncode)
        {
            var filePath = GetCachedNovelInfoSourceFilePath(SourceCacheDirectoryPath, ncode);
            if (File.Exists(filePath))
            {
                var yamlInfoAnalyzer = new YamlInfoAnalyzer(File.ReadAllText(filePath));
                if (yamlInfoAnalyzer.IsSingleData)
                {
                    var yamlWorkInfoAnalyzer = yamlInfoAnalyzer.GetYamlWorkInfoAnalyzer(ncode);
                    File.WriteAllLines(GetInfoDataTextFilePath(OutputDirectoryPath, ncode, true), DataUtil.GetLines(yamlWorkInfoAnalyzer.InfoMap));
                    return true;
                }
            }

            Logger.Warn($"{ncode}は存在しません。");
            return false;
        }

        #endregion

        #region アクセスデータ

        #region ソース（もとのHTMLファイル）

        #region 取得

        public bool TryGetPartialDataList(
            string ncode, DateTime[] dates, [NotNullWhen(true)] out int? maxNumber,
            [NotNullWhen(true)] out List<Tuple<DateTime, int, int>>? dataList)
        {
            var sourceList = new List<string>();
            foreach (var source in GetPartialAnalysisSources(ncode, dates))
                sourceList.Add(source);

            var _dataList = new List<Tuple<DateTime, int, int>>();
            var _maxNumber = default(int?);
            foreach (var source in sourceList)
            {
                if (TryParsePartialDataSource(source, out _, out _, out var targetMaxNumber, out var parsedDataList))
                {
                    if (!_maxNumber.HasValue || _maxNumber.Value < targetMaxNumber.Value)
                        _maxNumber = targetMaxNumber;
                    _dataList.AddRange(parsedDataList);
                }
            }

            if (_maxNumber.HasValue)
            {
                maxNumber = _maxNumber;
                dataList = _dataList;
                return true;
            }
            else
            {
                maxNumber = default;
                dataList = default;
                return false;
            }
        }

        private IEnumerable<string> GetPartialAnalysisSources(string ncode, DateTime[] dates)
        {
            foreach (var date in dates)
            {
                var filePath = GetCachedPartialAnalysisSourceFilePath(SourceCacheDirectoryPath, ncode, date);
                if (File.Exists(filePath))
                {
                    var source = File.ReadAllText(filePath);
                    if (IsInvalidSource(source))
                        Logger.Error($"無効な部分別ファイルがあります。コード：{ncode}　日付：{date.ToString(DateFormat)}");
                    yield return source;
                }
                else
                    Logger.Warn($"部分別ファイルがありません。コード：{ncode}　日付：{date.ToString(DateFormat)}");
            }
        }

        #endregion

        #endregion

        #region データ（加工CSVファイル）

        #region 出力

        public static readonly string[] PVAndUADataHeaders = { "PP", "PF", "PS", "UP", "UF", "US" };

        public void UpdateAccessData(string ncode, DateTime[] dates)
        {
            var map = WorkDataAnalyzer.GetPartialUniqueAccessCountsMap(ncode, OutputDirectoryPath, DateFormat);
            var noDataDates = dates.Where(x => !map.ContainsKey(x)).ToArray();
            if (TryGetPartialDataList(ncode, noDataDates, out var maxNumber, out var dataList))
            {
                #region ローカル関数

                static Dictionary<DateTime, int[]> CreatePartialUniqueAccessCountsMap(List<Tuple<DateTime, int, int>> partialDataList, int maxNumber)
                {
                    var map = new Dictionary<DateTime, int[]>();
                    foreach (var partialData in partialDataList)
                    {
                        var ncode = partialData.Item1;
                        if (!map.TryGetValue(ncode, out var numbers))
                            map.Add(ncode, numbers = new int[maxNumber]);

                        if (partialData.Item2 > 0)
                            numbers[partialData.Item2 - 1] = partialData.Item3;
                    }
                    return map;
                }

                static void AddPartialUniqueAccessCountsMap(
                    Dictionary<DateTime, int[]> target, IEnumerable<KeyValuePair<DateTime, int[]>> newDataValues)
                {
                    foreach (var newData in newDataValues)
                        target[newData.Key] = newData.Value;
                }

                #endregion

                AddPartialUniqueAccessCountsMap(map, CreatePartialUniqueAccessCountsMap(dataList, maxNumber.Value));

                var filePath = GetAccessDataCsvFilePath(OutputDirectoryPath, ncode, true);

                #region 出力関数

                IEnumerable<string> GetAccessOutputLines(Dictionary<DateTime, int[]> partialUniqueAccessCountsMap)
                {
                    var dateTimes = partialUniqueAccessCountsMap.Keys.ToArray();
                    Array.Sort(dateTimes);

                    if (dateTimes.Length > 0)
                    {
                        var numberCount = partialUniqueAccessCountsMap.Values.Select(x => x.Length).Max();
                        string GetTextLine(DateTime date, int[] pvCounts, int[] uaCounts, int[] partialAccessCounts)
                        {
                            var values = new string[numberCount + pvCounts.Length + uaCounts.Length + 1];
                            values[0] = date.ToString(DateFormat);
                            var shift = 1;
                            for (var i = 0; i < pvCounts.Length; i++)
                                values[i + shift] = string.Empty;
                            shift += pvCounts.Length;
                            for (var i = 0; i < uaCounts.Length; i++)
                                values[i + shift] = string.Empty;
                            shift += uaCounts.Length;
                            for (var i = 0; i < partialAccessCounts.Length; i++)
                                values[i + shift] = partialAccessCounts[i].ToString();
                            const string zeroText = "0";
                            for (var i = partialAccessCounts.Length; i < numberCount; i++)
                                values[i + shift] = zeroText;
                            return string.Join(",", values);
                        }

                        var headers = new string[numberCount + PVAndUADataHeaders.Length + 1];
                        headers[0] = string.Empty;
                        var shift = 1;
                        for (var i = 0; i < PVAndUADataHeaders.Length; i++)
                            headers[i + shift] = PVAndUADataHeaders[i];
                        shift += PVAndUADataHeaders.Length;
                        for (var i = 0; i < numberCount; i++)
                            headers[i + shift] = (i + 1).ToString();
                        yield return string.Join(",", headers);
                        for (var i = 0; i < dateTimes.Length; i++)
                            yield return GetTextLine(dateTimes[i], new int[3], new int[3], partialUniqueAccessCountsMap[dateTimes[i]]);
                    }
                }

                #endregion

                File.WriteAllLines(filePath, GetAccessOutputLines(map));
                Logger.Info($"{Path.GetFileName(filePath)}を出力しました。");
            }
            else
                Logger.Info($"更新可能な{ncode}のデータはありませんでした。");

            var hasError = HasError();
            if (hasError) Logger.Error("エラーが発生しました。ログを確認してください。");
        }

        #endregion

        #endregion

        #endregion
    }
}