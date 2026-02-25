using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace PomodoroTimer.Utils
{
    /// <summary>
    /// 静态工具类 - 在代码中获取本地化字符串
    /// 所有方法均绕过 Unity Smart Format，使用 entry.Value 读取原始值
    /// 含 {variable} 的条目无需勾选 Smart 复选框
    /// </summary>
    public static class LocalizedText
    {
        /// <summary>
        /// 获取简单本地化字符串（用于不含 {变量} 的条目）
        /// </summary>
        public static string Get(string tableName, string entryKey)
        {
            var entry = GetEntry(tableName, entryKey);
            if (entry == null)
                return entryKey;

            // 如果被误标为 Smart，主动清除，防止 LocalizeStringEvent 触发 FormattingException
            if (entry.IsSmart)
                entry.IsSmart = false;

            return entry.Value ?? entryKey;
        }

        /// <summary>
        /// 获取带变量替换的本地化字符串
        /// 读取原始值后手动替换 {key} 占位符，不依赖 Unity Smart Format
        /// </summary>
        public static string GetSmart(string tableName, string entryKey, params (string key, object value)[] args)
        {
            var entry = GetEntry(tableName, entryKey);
            if (entry == null)
                return entryKey;

            // 强制关闭 Smart Format，防止 LocalizeStringEvent 组件尝试解析 {variable} 时抛异常
            if (entry.IsSmart)
                entry.IsSmart = false;

            string rawString = entry.Value;
            if (string.IsNullOrEmpty(rawString))
                return entryKey;

            if (args == null || args.Length == 0)
                return rawString;

            string result = rawString;
            for (int i = 0; i < args.Length; i++)
            {
                result = result.Replace("{" + args[i].key + "}", args[i].value?.ToString() ?? "");
            }
            return result;
        }

        /// <summary>
        /// 获取 StringTableEntry，失败返回 null
        /// </summary>
        private static StringTableEntry GetEntry(string tableName, string entryKey)
        {
            var table = LocalizationSettings.StringDatabase.GetTable(tableName);
            if (table == null)
                return null;

            return table.GetEntry(entryKey);
        }
    }
}
