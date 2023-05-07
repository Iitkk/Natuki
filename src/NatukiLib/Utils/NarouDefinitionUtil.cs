namespace NatukiLib.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class NarouDefinitionUtil
    {
        #region Ncode

        public static bool IsNcode(string target) => Regex.IsMatch(target, @"^n\d{4}[a-z]{1,2}$");

        public static string GetNcode(int index)
        {
            var number = (index - 1) % 9999 + 1;
            var alphabetNumber = (index - 1) / 9999;
            var secondValue = alphabetNumber / 26;
            return "n" + number.ToString("0000")
                 + (secondValue > 0 ? ((char)(secondValue + 0x61)).ToString() : string.Empty)
                + ((char)(alphabetNumber % 26 + 0x61)).ToString();
        }

        public static int GetIndex(string ncode, bool isTextOrder = false)
        {
            var _ncode = ncode.ToLower();
            if (!IsNcode(_ncode)) throw new NotSupportedException();
            var number = ((_ncode.Length > 6 ? (int)(_ncode[5] - 0x61) * 26 : 0) + _ncode.Last() - 0x61);
            return isTextOrder ? int.Parse(ncode.AsSpan(1, 4)) * 26 * 26 + number : (number * 9999 + int.Parse(ncode.AsSpan(1, 4)));
        }

        #endregion

        #region ジャンル・作品特性

        private static readonly Dictionary<string, string> GenreMap = new Dictionary<string, string>()
        {
            { "101", "異世界〔恋愛〕" },
            { "102", "現実世界〔恋愛〕" },
            { "201", "ハイファンタジー〔ファンタジー〕" },
            { "202", "ローファンタジー〔ファンタジー〕" },
            { "301", "純文学〔文芸〕" },
            { "302", "ヒューマンドラマ〔文芸〕" },
            { "303", "歴史〔文芸〕" },
            { "304", "推理〔文芸〕" },
            { "305", "ホラー〔文芸〕" },
            { "306", "アクション〔文芸〕" },
            { "307", "コメディー〔文芸〕" },
            { "401", "VRゲーム〔SF〕" },
            { "402", "宇宙〔SF〕" },
            { "403", "空想科学〔SF〕" },
            { "404", "パニック〔SF〕" },
            { "9901", "童話〔その他〕" },
            { "9902", "詩〔その他〕" },
            { "9903", "エッセイ〔その他〕" },
            { "9904", "リプレイ〔その他〕" },
            { "9999", "その他〔その他〕" },
            { "9801", "ノンジャンル〔ノンジャンル〕" },
        };

        private static readonly Dictionary<string, string> R18GenreMap = new Dictionary<string, string>()
        {
            { "1", "ノクターンノベルズ(男性向け)" },
            { "2", "ムーンライトノベルズ(女性向け)" },
            { "3", "ムーンライトノベルズ(BL)" },
            { "4", "ミッドナイトノベルズ(大人向け)" },
        };

        public static string GetGenreName(string genreCode) => GenreMap[genreCode];

        public static string GetR18GenreName(string genreCode) => R18GenreMap[genreCode];

        public static string GetWorkPropertyText(
            int novelType, bool isNotCompleted, int storyNumber, bool isSuspended, DateTime firstUploadDateTime, DateTime lastUpdatedDateTime,
            bool isR15, bool isBoysLove, bool isGirlsLove, bool hasCruelDepiction, bool isIsekaiTensei, bool isIsekaiTenni,
            double conversationRate, double pictureCount)
        {
            var textList = new List<string>();

            static string GetTimePeriodText(DateTime startDateTime, DateTime endDateTime)
            {
                static int GetMonthCount(DateTime startDateTime, DateTime endDateTime, out int dayCount, out string inDayText)
                {
                    var monthCount = endDateTime.Year * 12 + endDateTime.Month - (startDateTime.Year * 12 + startDateTime.Month) + (endDateTime.Day >= startDateTime.Day ? 0 : -1);
                    if (monthCount == 0)
                    {
                        var diff = endDateTime - startDateTime;
                        if (diff.TotalDays >= 1)
                        {
                            dayCount = (int)(endDateTime.Date - startDateTime.Date).TotalDays;
                            inDayText = string.Empty;
                        }
                        else
                        {
                            dayCount = 0;
                            inDayText = (diff.TotalHours >= 1 ? Math.Floor(diff.TotalHours) + "時間" : (Math.Floor(diff.TotalMinutes) + "分"));
                        }
                    }
                    else
                    {
                        dayCount = 0;
                        inDayText = string.Empty;
                    }

                    return monthCount;
                }

                var monthCount = GetMonthCount(startDateTime, endDateTime, out var dayCount, out var inDayText);
                var year = monthCount / 12;
                var month = monthCount % 12;
                return (year > 0 ? +year + "年" : string.Empty) + (year <= 1 && month != 0 ? +month + "カ月" : string.Empty)
                    + (monthCount == 0 ? dayCount > 0 ? dayCount + "日" : inDayText : string.Empty);
            }

            var serializationText = GetTimePeriodText(firstUploadDateTime, lastUpdatedDateTime);
            var suspendedText = isSuspended ? "、" + GetTimePeriodText(lastUpdatedDateTime, DateTime.Now) + "未更新" : string.Empty;

            textList.Add(novelType == 1 ? $"{(isNotCompleted ? "連載中" : "完結")}（全{storyNumber}話、連載期間：{serializationText}{suspendedText}）" : "短編");
            if (isR15) textList.Add("R15");
            if (isBoysLove) textList.Add("ボーイズラブ");
            if (isGirlsLove) textList.Add("ガールズラブ");
            if (hasCruelDepiction) textList.Add("残酷な描写あり");
            if (isIsekaiTensei) textList.Add("異世界転生");
            if (isIsekaiTenni) textList.Add("異世界転移");
            textList.Add("会話率：" + conversationRate.ToString("N0") + "%");
            textList.Add("挿絵数：" + pictureCount.ToString("N0"));
            return string.Join("、", textList);
        }

        #endregion
    }
}
