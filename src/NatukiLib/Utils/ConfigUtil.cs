using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatukiLib.Utils
{
    public static class ConfigUtil
    {
        public static Dictionary<string, string> ConfigValueMap { get; }

        public static readonly string ConfigFileName = "Natuki.config.txt";

        static ConfigUtil() => ConfigValueMap = DataUtil.CreateValueMap(ConfigFileName);

        public static string? GetValueOrDefault(string name) => ConfigValueMap.GetValueOrDefault(name);

        public static string GetValueOrDefault(string name, string defaultValue) => ConfigValueMap.GetValueOrDefault(name, defaultValue);

        public static bool GetValueOrDefault(string name, bool defaultValue) => ConfigValueMap.TryGetValue(name, out var value) ? bool.Parse(value) : defaultValue;

        public static int GetValueOrDefault(string name, int defaultValue) => ConfigValueMap.TryGetValue(name, out var value) ? int.Parse(value) : defaultValue;

        public static void Set(string name, object value) => ConfigValueMap[name] = value.ToString() ?? string.Empty;

        public static void Update() => File.WriteAllLines(ConfigFileName, DataUtil.GetLines(ConfigValueMap));
    }
}
