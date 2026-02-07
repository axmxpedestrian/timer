using UnityEngine;

namespace PomodoroTimer.Resource
{
    /// <summary>
    /// 代币计算器
    /// 统一管理代币的计算逻辑
    /// </summary>
    public static class CoinCalculator
    {
        /// <summary>
        /// 计算番茄钟获得的代币数量
        /// 公式: y = 0.5 * (x - 4), 10 ≤ x ≤ 120; y = 60, x ≥ 120
        /// </summary>
        /// <param name="minutes">专注时长（分钟）</param>
        /// <returns>获得的代币数量</returns>
        public static int CalculateCoins(float minutes)
        {
            if (minutes < 10)
                return 0;

            if (minutes >= 120)
                return 60;

            // y = 0.5 * (x - 4)
            float coins = 0.5f * (minutes - 4);
            return Mathf.RoundToInt(coins);
        }

        /// <summary>
        /// 计算番茄钟获得的代币数量（从秒数）
        /// </summary>
        /// <param name="seconds">专注时长（秒）</param>
        /// <returns>获得的代币数量</returns>
        public static int CalculateCoinsFromSeconds(float seconds)
        {
            return CalculateCoins(seconds / 60f);
        }

        /// <summary>
        /// 获取下一个代币里程碑所需的分钟数
        /// </summary>
        /// <param name="currentMinutes">当前分钟数</param>
        /// <returns>下一个里程碑分钟数，如果已达最大则返回-1</returns>
        public static int GetNextMilestone(float currentMinutes)
        {
            if (currentMinutes < 10) return 10;
            if (currentMinutes < 25) return 25;
            if (currentMinutes < 50) return 50;
            if (currentMinutes < 90) return 90;
            if (currentMinutes < 120) return 120;
            return -1;
        }

        /// <summary>
        /// 获取代币计算公式说明
        /// </summary>
        public static string GetFormulaDescription()
        {
            return "专注10分钟起获得代币\n" +
                   "公式: 代币 = 0.5 × (分钟 - 4)\n" +
                   "最高: 120分钟 = 60代币";
        }
    }
}
