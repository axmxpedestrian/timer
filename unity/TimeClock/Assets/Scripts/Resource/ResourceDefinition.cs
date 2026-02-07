using System;
using UnityEngine;

namespace PomodoroTimer.Resource
{
    /// <summary>
    /// 资源类型枚举
    /// </summary>
    public enum ResourceType
    {
        Coin = 0,           // 代币
        Food = 1,           // 粮食
        Labor = 2,          // 劳动力
        Wood = 3,           // 木材
        Mineral = 4,        // 矿物
        Storage = 5,        // 存储容量
        Energy = 6,         // 能源
        Transport = 7,      // 运输
        Education = 8,      // 教育
        Scenery = 9,        // 风景
        Welfare = 10,       // 福利
        Productivity = 11,  // 生产力
        Research = 12       // 科研
    }

    /// <summary>
    /// 资源定义 - ScriptableObject
    /// 定义单个资源类型的属性
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceDefinition", menuName = "Resource/Resource Definition")]
    public class ResourceDefinition : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("资源类型")]
        public ResourceType resourceType;

        [Tooltip("资源名称")]
        public string resourceName;

        [Tooltip("资源描述")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("资源图标")]
        public Sprite icon;

        [Header("显示设置")]
        [Tooltip("显示顺序（越小越靠前）")]
        public int displayOrder;

        [Tooltip("图标颜色")]
        public Color iconColor = Color.white;

        [Header("初始状态")]
        [Tooltip("是否默认解锁（游戏开始时就显示）")]
        public bool unlockedByDefault = false;

        [Tooltip("初始数量")]
        public long initialAmount = 0;

        /// <summary>
        /// 格式化数量显示
        /// </summary>
        public static string FormatAmount(long amount)
        {
            if (amount < 0)
            {
                return "-" + FormatAmount(-amount);
            }

            if (amount < 1000)
            {
                return amount.ToString();
            }
            else if (amount < 1000000)
            {
                // K: 千
                float value = amount / 1000f;
                return FormatWithSuffix(value, "K");
            }
            else if (amount < 1000000000)
            {
                // M: 百万
                float value = amount / 1000000f;
                return FormatWithSuffix(value, "M");
            }
            else if (amount < 1000000000000)
            {
                // B: 十亿
                float value = amount / 1000000000f;
                return FormatWithSuffix(value, "B");
            }
            else
            {
                // T: 万亿
                float value = amount / 1000000000000f;
                return FormatWithSuffix(value, "T");
            }
        }

        private static string FormatWithSuffix(float value, string suffix)
        {
            if (value >= 100)
            {
                return $"{value:F0}{suffix}";
            }
            else if (value >= 10)
            {
                return $"{value:F1}{suffix}";
            }
            else
            {
                return $"{value:F2}{suffix}";
            }
        }

        /// <summary>
        /// 格式化带符号的数量变化
        /// </summary>
        public static string FormatAmountChange(long amount)
        {
            string formatted = FormatAmount(Math.Abs(amount));
            if (amount > 0)
                return "+" + formatted;
            else if (amount < 0)
                return "-" + formatted;
            return formatted;
        }
    }
}
