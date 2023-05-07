
namespace NatukiLib.ViewModels
{
    using NatukiLib;
    using NatukiLib.Controls;
    using NatukiLib.Utils;
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public class MainViewModel : INotifyPropertyChanged
    {
        #region 静的関数

        private static string? ToNullWhenEmpty(string target) => target.Length == 0 ? null : target;

        #endregion

        #region コンストラクタ

        public MainViewModel(Action<MainViewModel> updateDataAction, Func<object?, object?> queryFunc)
        {
            UpdateDataAction = updateDataAction;
            QueryFunc = queryFunc;

            ViewDataTypeTexts = new[] { "ユニーク合計", "継続率", "離脱率", "前話継続率", "前話離脱率" };
            sourcePath = ConfigUtil.GetValueOrDefault(nameof(SourcePath), CommonUtil.DefaultSourceDirectoryPath);
            dataPath = ConfigUtil.GetValueOrDefault(nameof(DataPath), CommonUtil.DefaultDataDirectoryPath);
            isTimeSortedNcode = ConfigUtil.GetValueOrDefault(nameof(IsTimeSortedNcode), false);
            enableR18 = ConfigUtil.GetValueOrDefault(nameof(EnableR18), false);
            Ncodes = new ObservableCollection<string>(DataUtil.GetNcodesInDataDirectory(OutputDirectoryPath)
                .OrderBy(x => NarouDefinitionUtil.GetIndex(x, !IsTimeSortedNcode)).ToArray());
            ncode = ConfigUtil.GetValueOrDefault(nameof(Ncode), Ncodes.FirstOrDefault() ?? string.Empty);
            progressText = string.Empty;
            enableOperation = true;
            isR18 = false;
            SynchronizationContext = SynchronizationContext.Current ?? throw new NotSupportedException();
            titleName = workExplanation = bookmarkText = totalPointText = ratingInfoText = dateInfoText = string.Empty;
            workUrl = startStoryNumberText = string.Empty;
            viewDataInfoText = string.Empty;
            downloadInterval = ConfigUtil.GetValueOrDefault(nameof(DownloadInterval), CommonUtil.DefaultDownloadInterval);
            cancelingDownloadDuration = ConfigUtil.GetValueOrDefault(nameof(CancelingDownloadDuration), CommonUtil.DefaultDownloadTimeOut);
            httpClientUserAgent = ConfigUtil.GetValueOrDefault(nameof(HttpClientUserAgent), CommonUtil.DefaultHttpClientUserAgent);

            Nubmers = Array.Empty<int>();
            PartialUniqueAccessCountValues = Array.Empty<double>();
            ViewDataXValues = ViewDataYValues = Array.Empty<double>();
            GetWorkDataAnalyzerAndDate(Ncode, DataPath, out var workDataAnalyzer, out startDate, out endDate);
            WorkDataAnalyzer = workDataAnalyzer;
            StartStoryNumberTexts = new ObservableCollection<string>();
            UpdateWorkInfo();

            UpdateConfigCommand = new AsyncDelegateCommand(async x =>
            {
                if ((bool)QueryFunc(new object[] { $"設定を更新しますか？", "データ", true })!)
                {
                    ConfigUtil.Set(nameof(Ncode), Ncode);
                    ConfigUtil.Set(nameof(SourcePath), SourcePath);
                    ConfigUtil.Set(nameof(DataPath), DataPath);
                    ConfigUtil.Set(nameof(IsTimeSortedNcode), IsTimeSortedNcode);
                    ConfigUtil.Set(nameof(EnableR18), EnableR18);
                    ConfigUtil.Set(nameof(DownloadInterval), DownloadInterval);
                    ConfigUtil.Set(nameof(CancelingDownloadDuration), CancelingDownloadDuration);
                    ConfigUtil.Set(nameof(HttpClientUserAgent), HttpClientUserAgent);
                    await Task.Run(() => ConfigUtil.Update());
                }
            });
            OpenWorkPageCommand = new DelegateCommand(x => Process.Start(new ProcessStartInfo(WorkUrl) { UseShellExecute = true }));
            UpdateViewDataLater(true);
        }

        #endregion

        #region 共通

        private Action<MainViewModel> UpdateDataAction { get; }

        private Func<object?, object?> QueryFunc { get; }

        private SynchronizationContext SynchronizationContext { get; init; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void UpdateProperty([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private bool Update<T>(ref T target, T value, Action<T>? action = null, [CallerMemberName] string? propertyName = null)
        {
            if (!Equals(target, value))
            {
                target = value;
                action?.Invoke(target);
                UpdateProperty(propertyName);
                return true;
            }
            else
                return false;
        }

        private string progressText;
        public string ProgressText { get => progressText; set => Update(ref progressText, value); }

        private bool enableOperation;
        public bool EnableOperation { get => enableOperation; set => Update(ref enableOperation, value); }

        #endregion

        #region 設定

        private string sourcePath;
        public string SourcePath { get => sourcePath; set => Update(ref sourcePath, value); }

        private string dataPath;
        public string DataPath { get => dataPath; set => Update(ref dataPath, value, x => UpdateWorkDataAnalyzer()); }

        private bool isTimeSortedNcode;
        public bool IsTimeSortedNcode { get => isTimeSortedNcode; set => Update(ref isTimeSortedNcode, value); }

        private bool enableR18;
        public bool EnableR18 { get => enableR18; set => Update(ref enableR18, value); }

        #region Debug

        public bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        private int downloadInterval;
        public int DownloadInterval { get => downloadInterval; set => Update(ref downloadInterval, value); }

        private int cancelingDownloadDuration;
        public int CancelingDownloadDuration { get => cancelingDownloadDuration; set => Update(ref cancelingDownloadDuration, value); }

        private string httpClientUserAgent;
        public string HttpClientUserAgent { get => httpClientUserAgent; set => Update(ref httpClientUserAgent, value); }

        #endregion

        #region Private

        private string? SourceCacheDirectoryPath => ToNullWhenEmpty(SourcePath);

        private string? OutputDirectoryPath => ToNullWhenEmpty(DataPath);

        #endregion

        #endregion

        #region 情報

        private string ncode;
        public string Ncode
        {
            get => ncode; set => Update(ref ncode, value.ToLower(), x =>
            {
                UpdateNcodes();
                UpdateWorkDataAnalyzer(true);
            });
        }

        private bool isUpdatingNcodes;
        private void UpdateNcodes()
        {
            if (!isUpdatingNcodes)
            {
                isUpdatingNcodes = true;
                var newNcodes = DataUtil.GetNcodesInDataDirectory(OutputDirectoryPath).OrderBy(x => NarouDefinitionUtil.GetIndex(x, !IsTimeSortedNcode)).ToArray();
                var ncodes = Ncodes;
                var ncode = Ncode;
                if (!newNcodes.SequenceEqual(ncodes))
                    SynchronizationContext.Post(_ =>
                    {
                        ncodes.Clear();
                        foreach (var newNcode in newNcodes)
                            ncodes.Add(newNcode);
                        Ncode = ncode;
                        isUpdatingNcodes = false;
                    }, null);
                else
                    isUpdatingNcodes = false;
            }
        }

        public ObservableCollection<string> Ncodes { get; private set; }

        private DateTime startDate;
        public DateTime StartDate { get => startDate; set => Update(ref startDate, value, x => UpdateViewDataLater()); }

        private DateTime endDate;
        public DateTime EndDate { get => endDate; set => Update(ref endDate, value, x => UpdateViewDataLater()); }

        #endregion

        #region 作品データ

        public WorkDataAnalyzer? WorkDataAnalyzer { get; set; }

        private void UpdateWorkDataAnalyzer(bool disableUpdate = false)
        {
            GetWorkDataAnalyzerAndDate(Ncode, DataPath, out var workDataAnalyzer, out var _startDate, out var _endDate);
            WorkDataAnalyzer = workDataAnalyzer;
            UpdateViewDataLater(disableUpdate);
            StartDate = _startDate;
            EndDate = _endDate;
            UpdateWorkInfo();
        }

        private void UpdateWorkInfo()
        {
            var workDataAnalyzer = WorkDataAnalyzer;
            if (workDataAnalyzer is not null && workDataAnalyzer.HasInfo)
            {
                static string AddNewLine(string target)
                {
                    var maxLineLength = 100;
                    var index = 0;
                    var nextNewLine = 0;
                    var text = target.Replace("\n", Environment.NewLine).Replace("\r\n\n", Environment.NewLine);
                    var sb = new StringBuilder();
                    while (index < text.Length)
                    {
                        if (nextNewLine != -1 && nextNewLine <= index)
                            nextNewLine = text.IndexOf(Environment.NewLine, nextNewLine);

                        if (nextNewLine == -1 || index + maxLineLength < nextNewLine)
                        {
                            sb.Append(text.AsSpan(index, Math.Min(text.Length - index, maxLineLength)));
                            sb.Append(Environment.NewLine);
                            index += maxLineLength;
                        }
                        else
                        {
                            var afterNewLineIndex = nextNewLine + Environment.NewLine.Length;
                            sb.Append(text.AsSpan(index, afterNewLineIndex - index));
                            index = nextNewLine = afterNewLineIndex;
                        }
                    }

                    return sb.ToString();
                }

                static string Join(params string[] values) => string.Join(Environment.NewLine, values);

                string GetText(string name) => workDataAnalyzer.GetValueOrDefault(name, string.Empty);
                int GetValue(string name) => int.Parse(GetText(name));
                bool GetBooleanValue(string name, bool? defaultValue = null)
                    => int.TryParse(GetText(name), out var value) ? value != 0 : defaultValue ?? throw new NotSupportedException(); ;
                string FormatText(int value) => value.ToString("N0");
                string GetValueText(string name) => FormatText(GetValue(name));
                DateTime GetDateTime(string name) => DateTime.Parse(GetText(name));

                var firstUploadDateTime = GetDateTime("general_firstup");
                var lastUpdatedDateTime = GetDateTime("general_lastup");

                #region 作品情報

                {
                    IsR18 = workDataAnalyzer.InfoMap.ContainsKey("nocgenre");
                    var readingMinutes = GetValue("time");
                    var charactorCount = GetValue("length");
                    var storyNumber = GetValue("general_all_no");
                    WorkExplanation = Join(
                        AddNewLine(TitleName = GetText("title")),
                        "作者：" + GetText("writer"),
                        "キーワード：" + GetText("keyword"),
                        "ジャンル：" + (isR18 ? NarouDefinitionUtil.GetR18GenreName(GetText("nocgenre")) : NarouDefinitionUtil.GetGenreName(GetText("genre"))),
                        "作品属性：" + NarouDefinitionUtil.GetWorkPropertyText(
                            GetValue("novel_type"), GetBooleanValue("end"), storyNumber, GetBooleanValue("isstop"), firstUploadDateTime, lastUpdatedDateTime,
                            GetBooleanValue("isr15", false), GetBooleanValue("isbl"), GetBooleanValue("isgl"), GetBooleanValue("iszankoku"), GetBooleanValue("istensei"),
                            GetBooleanValue("istenni"), GetValue("kaiwaritu"), GetValue("sasie_cnt")),
                        "文字数：" + FormatText(charactorCount) + "文字"
                        + "（読了時間［500文字/分］：" + (readingMinutes >= 60 ? (readingMinutes / 60) + "時間" : string.Empty) + (readingMinutes % 60) + "分、" +
                        "一話平均：" + FormatText(charactorCount / storyNumber) + "文字）",
                        "あらすじ：" + AddNewLine(GetText("story")));
                    WorkUrl = $"https://ncode.syosetu.com/{Ncode}/";
                }

                #endregion

                #region 評価情報

                {
                    var bookmarkCount = GetValue("fav_novel_cnt");
                    var totalPointValue = GetValue("global_point");
                    var ratingCount = GetValue("all_hyoka_cnt");
                    RatingInfoText = Join(
                        "ブックマーク：" + (BookmarkText = FormatText(bookmarkCount) + "件"),
                        "総合評価：" + (TotalPointText = FormatText(totalPointValue) + "pt"),
                        "評価ポイント：" + GetValueText("all_point") + "pt",
                        "評価人数：" + FormatText(ratingCount) + "人",
                        "平均点：" + (ratingCount != 0 ? (GetValue("all_point") / (double)ratingCount).ToString("N2") : "-"),
                        "感想：" + GetValueText("impression_cnt") + "件",
                        "レビュー：" + GetValueText("review_cnt") + "件",
                        "週別ユニークユーザ：" + GetValueText("weekly_unique") + "人",
                        "期間ポイント：" + GetValueText("daily_point") + "pt/日　" + GetValueText("weekly_point") + "pt/週　" + GetValueText("monthly_point") + "pt/月　"
                         + GetValueText("quarter_point") + "pt/四半期　" + GetValueText("yearly_point") + "pt/年",
                        "ブックマークに対する評価率：" + (bookmarkCount > 0 ? (ratingCount / (double)bookmarkCount).ToString("P02") : "-"));
                }

                #endregion

                #region 日時情報

                {
                    string dateFormat = "yyyy/MM/dd hh:mm:ss";
                    string GetDateText(string name) => GetDateTime(name).ToString(dateFormat);
                    DateInfoText = Join(
                        "初回掲載日時：" + firstUploadDateTime.ToString(dateFormat),
                        "最新掲載日時：" + lastUpdatedDateTime.ToString(dateFormat),
                        "最新更新日時：" + GetDateText("novelupdated_at"),
                        "データ更新日時：" + GetDateText("updated_at"));
                    var firstUploadDate = firstUploadDateTime.Date;
                    if (startDate < firstUploadDate)
                        Update(ref startDate, firstUploadDate, propertyName: nameof(StartDate));
                }

                #endregion
            }
            else
                TitleName = WorkUrl = WorkExplanation = BookmarkText = TotalPointText = RatingInfoText = DateInfoText = string.Empty;
            UpdateStartStoryNumber();
        }
        private void UpdateStartStoryNumber()
        {
            var workDataAnalyzer = WorkDataAnalyzer;
            if (workDataAnalyzer is not null && workDataAnalyzer.HasInfo)
            {
                var startStoryNumberTexts = Enumerable.Range(
                    1, int.Parse(workDataAnalyzer.GetValueOrDefault("general_all_no", string.Empty))).Select(x => x.ToString()).ToArray();

                if (!startStoryNumberTexts.SequenceEqual(StartStoryNumberTexts))
                {
                    StartStoryNumberTexts.Clear();
                    foreach (var startStoryNumber in startStoryNumberTexts)
                        StartStoryNumberTexts.Add(startStoryNumber);
                    StartStoryNumberText = StartStoryNumberTexts.FirstOrDefault() ?? string.Empty;
                }
            }
            else
            {
                StartStoryNumberTexts.Clear();
                StartStoryNumberText = string.Empty;
            }
        }

        private void GetWorkDataAnalyzerAndDate(string ncode, string outputDirectoryPath, out WorkDataAnalyzer? workDataAnalyzer, out DateTime startDate, out DateTime endDate)
        {
            DateTime[] dates;
            if (NarouDefinitionUtil.IsNcode(ncode))
            {
                var _workDataAnalyzer = new WorkDataAnalyzer(ncode, outputDirectoryPath);
                dates = _workDataAnalyzer.PartialUniqueAccessCountDates;
                workDataAnalyzer = _workDataAnalyzer;
            }
            else
            {
                dates = Array.Empty<DateTime>();
                workDataAnalyzer = null;
            }
            DateTime yesterdayDateTime;
            (endDate, startDate) = dates.Length > 0 ? (dates.Last(), dates.First()) : (yesterdayDateTime = DateTime.Today.AddDays(-1), yesterdayDateTime);
        }

        private string titleName;
        public string TitleName { get => titleName; set => Update(ref titleName, value.Trim()); }

        private bool isR18;
        public bool IsR18 { get => isR18; set => Update(ref isR18, value); }

        private string workUrl;
        public string WorkUrl { get => workUrl; set => Update(ref workUrl, value.Trim()); }

        public ICommand OpenWorkPageCommand { get; }

        private string workExplanation;
        public string WorkExplanation { get => workExplanation; set => Update(ref workExplanation, value.Trim()); }

        private string bookmarkText;
        public string BookmarkText { get => bookmarkText; set => Update(ref bookmarkText, value); }

        private string totalPointText;
        public string TotalPointText { get => totalPointText; set => Update(ref totalPointText, value); }

        private string ratingInfoText;
        public string RatingInfoText { get => ratingInfoText; set => Update(ref ratingInfoText, value.Trim()); }

        private string dateInfoText;
        public string DateInfoText { get => dateInfoText; set => Update(ref dateInfoText, value); }

        private string startStoryNumberText;
        public string StartStoryNumberText
        {
            get => startStoryNumberText; set => Update(ref startStoryNumberText, value, x =>
            {
                UpdateWorkInfo();
                UpdateViewDataLater();
            });
        }
        public ObservableCollection<string> StartStoryNumberTexts { get; private set; }

        #endregion

        #region 表示データ

        #region 表示データタイプ

        public string[] ViewDataTypeTexts { get; init; }

        private int viewDataTypeIndex;
        public int ViewDataTypeIndex { get => viewDataTypeIndex; set => Update(ref viewDataTypeIndex, value, x => UpdateViewDataLater()); }

        public string ViewDataTypeText => ViewDataTypeTexts[ViewDataTypeIndex];

        public ViewDataType ViewDataType => (ViewDataType)ViewDataTypeIndex;

        #endregion

        #region データ

        public int[] Nubmers { get; set; }

        public double[] PartialUniqueAccessCountValues { get; set; }


        private void UpdateViewDataValues()
        {
            static double[] ToDoubles(IEnumerable<int> values) => values.Select(x => (double)x).ToArray();

            static (double[] ViewDataXValues, double[] ViewDataYValues) GetViewData(
                bool isAbandonmentRate, bool isOnPreviousStory, int[] nubmers, double[] partialUniqueAccessCountValues)
            {
                if (partialUniqueAccessCountValues.Length == 0) return (Array.Empty<double>(), Array.Empty<double>());

                var values = new double[partialUniqueAccessCountValues.Length - 1];
                var firstPartialUniqueAccessCountValue = partialUniqueAccessCountValues.First();
                for (var i = 0; i < values.Length; i++)
                {
                    var previousValue = isOnPreviousStory ? partialUniqueAccessCountValues[i] : firstPartialUniqueAccessCountValue;
                    if (previousValue == 0)
                        values[i] = 0;
                    else
                    {
                        var value = (partialUniqueAccessCountValues[i + 1] - previousValue) / previousValue;
                        values[i] = (isAbandonmentRate ? -value : (1 + value));
                    }
                }

                return (ToDoubles(nubmers.Take(nubmers.Length - 1)), values);
            }

            var skipNumber = int.TryParse(StartStoryNumberText, out var value) ? value - 1 : 0;
            var numbers = Nubmers.Skip(skipNumber).ToArray();
            var partialUniqueAccessCountValues = PartialUniqueAccessCountValues.Skip(skipNumber).ToArray();
            double[] viewDataXValues;
            double[] viewDataYValues;
            switch (ViewDataType)
            {
                case ViewDataType.UniqueAccess:
                    (viewDataXValues, viewDataYValues) = (ToDoubles(numbers), partialUniqueAccessCountValues);
                    break;
                case ViewDataType.RetentionRate:
                    (viewDataXValues, viewDataYValues) = GetViewData(false, false, numbers, partialUniqueAccessCountValues);
                    break;
                case ViewDataType.AbandonmentRate:
                    (viewDataXValues, viewDataYValues) = GetViewData(true, false, numbers, partialUniqueAccessCountValues);
                    break;
                case ViewDataType.RetentionRateOnPreviousStory:
                    (viewDataXValues, viewDataYValues) = GetViewData(false, true, numbers, partialUniqueAccessCountValues);
                    break;
                case ViewDataType.AbandonmentRateOnPreviousStory:
                    (viewDataXValues, viewDataYValues) = GetViewData(true, true, numbers, partialUniqueAccessCountValues);
                    break;
                default:
                    throw new NotSupportedException();
            }

            nullableViewDataXValues = viewDataXValues;
            nullableViewDataYValues = viewDataYValues;

            #region 表示データ情報

            {
                var workDataAnalyzer = WorkDataAnalyzer;
                if (workDataAnalyzer is not null && workDataAnalyzer.HasInfo)
                {
                    var bookmarkCount = int.Parse(workDataAnalyzer.GetValue("fav_novel_cnt"));
                    var uaPerBookmarkValue = partialUniqueAccessCountValues?.FirstOrDefault() ?? -1.0;
                    ViewDataInfoText = string.Join(
                        Environment.NewLine,
                        "ブックマーク１件当たりの開始話ユニークアクセス数："
                        + (bookmarkCount != 0 && uaPerBookmarkValue > 0
                        ? (uaPerBookmarkValue / bookmarkCount).ToString("N2")
                        + $"（開始話ユニークアクセスに対するブックマーク率：{bookmarkCount / uaPerBookmarkValue:P2}）"
                        : "-"),
                        "開始話からの平均継続話数：" + (uaPerBookmarkValue > 0 ? (partialUniqueAccessCountValues!.Sum() / uaPerBookmarkValue).ToString("N2") : "-")
                        );
                }
                else
                    ViewDataInfoText = "";
            }

            #endregion
        }

        public double[]? nullableViewDataXValues = null;
        public double[] ViewDataXValues
        {
            get
            {
                if (nullableViewDataXValues is null)
                    UpdateViewDataValues();
                return nullableViewDataXValues ?? throw new NotSupportedException();
            }
            set
            {
                UpdateProperty();
                UpdateViewDataLater();
            }
        }

        public double[]? nullableViewDataYValues = null;
        public double[] ViewDataYValues
        {
            get
            {
                if (nullableViewDataYValues is null)
                    UpdateViewDataValues();
                return nullableViewDataYValues ?? throw new NotSupportedException();
            }
            set
            {
                UpdateProperty();
                UpdateViewDataLater();
            }
        }

        private string viewDataInfoText;
        public string ViewDataInfoText { get => viewDataInfoText; set => Update(ref viewDataInfoText, value); }

        #endregion

        #region 更新

        private bool isUpdating;
        private void UpdateViewDataLater(bool disableUpdate = false)
        {
            if (!isUpdating)
            {
                isUpdating = true;
                SynchronizationContext.Post(async _ =>
                {
                    try
                    {
                        await UpdateViewData(null, disableUpdate);
                    }
                    catch (Exception e)
                    {
                        CommonUtil.Logger.Error(e.Message, e);
                        QueryFunc(new object[] { $"エラーが発生しました。処理を中止します。再度実行してください。\n\rエラー：{e.Message}", "エラー", false });
                    }
                }, null);
            }
        }

        private (string? Ncode, DateTime? EndDate)? previousSetting;
        private async Task UpdateViewData(object? parameter, bool disableUpdate = false)
        {
            if (isUpdating)
                try
                {
                    var workDataAnalyzer = WorkDataAnalyzer;
                    if (workDataAnalyzer is not null)
                    {
                        var startDate = StartDate;
                        var endDate = EndDate;

                        var uncovereedDates = workDataAnalyzer.GetUncoveredDates(startDate, endDate);

                        #region 前日対応

                        // 前日にデータがない場合、二回目以降はダウンロードしない。
                        // ただ、再度終了日を変えると、ダウンロードを試みる。

                        bool IsYesterdayChecked(ref DateTime[] uncovereedDates)
                        {
                            var result = previousSetting.HasValue && uncovereedDates.Length == 1
                                && uncovereedDates[0] == previousSetting.Value.EndDate && ncode == previousSetting.Value.Ncode;
                            if (!result) previousSetting = null;
                            else uncovereedDates = Array.Empty<DateTime>();
                            return result;
                        }

                        if (previousSetting.HasValue && (previousSetting.Value.EndDate != endDate || previousSetting.Value.Ncode != ncode))
                            previousSetting = null;

                        #endregion

                        if (!disableUpdate && uncovereedDates.Length > 0 && !IsYesterdayChecked(ref uncovereedDates)
                            && (bool)QueryFunc(new object[] { $"{uncovereedDates.Length}日分のデータがありませんが、ダウンロードしますか？", "データ", true })!)
                        {
                            try
                            {
                                EnableOperation = false;
                                ProgressText = "0/" + uncovereedDates.Length;

                                if (uncovereedDates.Length == 1 && uncovereedDates[0] >= DateTime.Today.AddDays(-1))
                                    previousSetting = (ncode, uncovereedDates[0]);
                                else
                                    previousSetting = null;

                                var counter = 0;
                                var taskHelper = new TaskHelper(Ncode, uncovereedDates, SourceCacheDirectoryPath, OutputDirectoryPath, x =>
                                {
                                    if (x is DateTime targetDate && Array.BinarySearch(uncovereedDates, targetDate) >= 0)
                                        ProgressText = (++counter).ToString() + "/" + uncovereedDates.Length;
                                }, EnableR18, DownloadInterval, CancelingDownloadDuration, HttpClientUserAgent);

                                await taskHelper.Run();
                                workDataAnalyzer.ClearCache();
                            }
                            finally
                            {
                                ProgressText = string.Empty;
                                EnableOperation = true;
                            }
                        }

                        (Nubmers, PartialUniqueAccessCountValues) = workDataAnalyzer.GetNumbersAndValues(StartDate, EndDate);
                    }
                    else
                        (Nubmers, PartialUniqueAccessCountValues) = (Array.Empty<int>(), Array.Empty<double>());

                    nullableViewDataXValues = null;
                    nullableViewDataYValues = null;
                    UpdateWorkInfo();
                    UpdateDataAction(this);
                }
                finally
                {
                    isUpdating = false;
                }
        }

        #endregion

        #endregion

        #region 設定

        public ICommand UpdateConfigCommand { get; }


        #endregion
    }
}
