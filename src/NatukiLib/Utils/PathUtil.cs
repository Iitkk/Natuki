namespace NatukiLib.Utils
{
    public static class PathUtil
    {
        public static string Combine(string directoryPath, string filePath, bool createsDirectory = false)
        {
            var path = Path.Combine(directoryPath, filePath);
            var targetDirectoryPath = Path.GetDirectoryName(path);
            if (createsDirectory && targetDirectoryPath is not null && !Directory.Exists(targetDirectoryPath))
                Directory.CreateDirectory(targetDirectoryPath);
            return path;
        }

        public static string? CreateDirectory(string filePath)
        {
            var directoryPath = Path.GetDirectoryName(filePath);
            DirectoryInfo directoryInfo;
            if (directoryPath is not null && !(directoryInfo = new DirectoryInfo(directoryPath)).Exists)
                directoryInfo.Create();
            return directoryPath;
        }

        public static string GetYearAndMonthDirectory(DateTime date) => Path.Combine(date.Year.ToString("0000"), date.Month.ToString("00"));
    }
}
