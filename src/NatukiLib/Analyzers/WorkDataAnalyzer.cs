namespace NatukiLib
{
    using NatukiLib.Utils;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class WorkDataAnalyzer
    {
        #region 静的関数

        public static Dictionary<DateTime, int[]> GetPartialUniqueAccessCountsMap(string ncode, string dataDirectoryPath, string dataFormat)
        {
            (DateTime[] dates, int[][] countArrays) = GetAccessDateAndCounts(ncode, dataDirectoryPath, dataFormat);
            var map = new Dictionary<DateTime, int[]>();
            for (var i = 0; i < dates.Length; i++)
                map.Add(dates[i], countArrays[i]);
            return map;
        }

        private static (DateTime[] accessDates, int[][] partialCountArrays) GetAccessDateAndCounts(string ncode, string dataDirectoryPath, string dataFormat)
        {
            var dateList = new List<DateTime>();
            var pvAndUAList = new List<int?[]>();
            var partialCountsList = new List<int[]>();
            var pvAndUAColumnCount = DataConverter.PVAndUADataHeaders.Length;
            var filePath = CommonUtil.GetAccessDataCsvFilePath(dataDirectoryPath, ncode);
            if (File.Exists(filePath))
                foreach (var textLine in File.ReadAllLines(filePath).Skip(1))
                {
                    var values = textLine.Split(",");
                    var shift = 0;
                    dateList.Add(DateTime.ParseExact(values[shift], dataFormat, null));
                    shift += 1;

                    var pvAndUACounts = new int?[pvAndUAColumnCount];
                    for (var i = 0; i < pvAndUACounts.Length; i++)
                        pvAndUACounts[i] = null;
                    pvAndUAList.Add(pvAndUACounts);
                    shift += pvAndUAColumnCount;

                    var paritalCounts = new int[values.Length - pvAndUAColumnCount - 1];
                    for (var i = 0; i < paritalCounts.Length; i++)
                        paritalCounts[i] = int.Parse(values[i + shift]);
                    partialCountsList.Add(paritalCounts);
                }
            return (dateList.ToArray(), partialCountsList.ToArray());
        }

        #endregion

        #region コンストラクタ

        public WorkDataAnalyzer(string ncode, string dataDirectoryPath, string? dataFormat = null)
        {
            Ncode = ncode;
            DataDirectoryPath = dataDirectoryPath;
            DateFormat = dataFormat ?? CommonUtil.DateFormat;
        }

        #endregion

        #region 共通

        public string Ncode { get; }

        public string DataDirectoryPath { get; }

        public string DateFormat { get; }

        public void ClearCache()
        {
            partialUniqueAccessDateAndCounts = null;
            infoMap = null;
        }

        #endregion

        #region 日付

        public DateTime[] GetUncoveredDates(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate) return Array.Empty<DateTime>();

            var dates = PartialUniqueAccessCountDates;

            var uncoveredDateList = new List<DateTime>();
            var currentDate = startDate;
            var binaryStartDateIndex = Array.BinarySearch(dates, startDate);
            var index = binaryStartDateIndex < 0 ? ~binaryStartDateIndex : binaryStartDateIndex;
            while (currentDate <= endDate)
            {
                if (index >= dates.Length || currentDate != dates[index])
                    uncoveredDateList.Add(currentDate);
                else
                    index++;

                currentDate = currentDate.AddDays(1);
            }

            return uncoveredDateList.ToArray();
        }

        #endregion

        #region 情報

        public bool HasInfo => InfoMap.Count > 0;

        private Dictionary<string, string>? infoMap;
        public Dictionary<string, string> InfoMap
        {
            get => infoMap ??= DataUtil.CreateValueMap(CommonUtil.GetInfoDataTextFilePath(DataDirectoryPath, Ncode));
        }

        public string GetValue(string name) => InfoMap[name];

        public string GetValueOrDefault(string name, string defautValue) => InfoMap.GetValueOrDefault(name, defautValue);

        #endregion

        #region 部分別

        private (DateTime[] PartialUniqueAccessCountDates, int[][] PartialUniqueAccessCountArrays)? partialUniqueAccessDateAndCounts;
        public DateTime[] PartialUniqueAccessCountDates
        {
            get
            {
                if (partialUniqueAccessDateAndCounts is null)
                    partialUniqueAccessDateAndCounts = GetAccessDateAndCounts(Ncode, DataDirectoryPath, DateFormat);
                return partialUniqueAccessDateAndCounts.Value.PartialUniqueAccessCountDates;
            }
        }

        public int[][] PartialUniqueAccessCountArrays
        {
            get
            {
                if (partialUniqueAccessDateAndCounts is null)
                    partialUniqueAccessDateAndCounts = GetAccessDateAndCounts(Ncode, DataDirectoryPath, DateFormat);
                return partialUniqueAccessDateAndCounts.Value.PartialUniqueAccessCountArrays;
            }
        }

        private int[]? numbers;
        private double[]? partialUniqueAccessCountValues;

        public int[] Numbers
        {
            get
            {
                if (numbers is null)
                    (numbers, partialUniqueAccessCountValues) = GetNumbersAndValues();
                return numbers;
            }
        }

        public double[] PartialUniqueAccessCountValues
        {
            get
            {
                if (partialUniqueAccessCountValues is null)
                    (numbers, partialUniqueAccessCountValues) = GetNumbersAndValues();
                return partialUniqueAccessCountValues;
            }
        }

        public (int[] Numbers, double[] PartialUniqueAccessCountValues) GetNumbersAndValues(DateTime? startDate = null, DateTime? endDate = null)
        {
            var dates = PartialUniqueAccessCountDates;

            int GetIndex(DateTime date, bool isEnd)
            {
                var index = Array.BinarySearch(dates, date);
                return index < 0 ? ~index + (isEnd ? -1 : 0) : index;
            }

            var startIndex = startDate.HasValue ? GetIndex(startDate.Value, false) : 0;
            var endIndex = endDate.HasValue ? GetIndex(endDate.Value, true) : dates.Length - 1;
            if (endIndex >= startIndex)
            {
                var countArrays = PartialUniqueAccessCountArrays.Skip(startIndex).Take(endIndex - startIndex + 1).ToArray();
                var numbers = new int[countArrays[0].Length];
                for (var i = 0; i < numbers.Length; i++)
                    numbers[i] = i + 1;
                var values = new double[countArrays[0].Length];
                for (var i = 0; i < countArrays.Length; i++)
                    for (var j = 0; j < countArrays[i].Length; j++)
                        values[j] += countArrays[i][j];

                return (numbers, values);
            }
            else
                return (Array.Empty<int>(), Array.Empty<double>());
        }

        #endregion
    }
}
