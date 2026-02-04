using UnityEngine;

namespace PomodoroTimer.Utils
{
    /// <summary>
    /// 颜色配置 - 用于任务标记颜色
    /// </summary>
    public static class ColorPalette
    {
        // 任务标记颜色 (6种)
        public static readonly Color[] TaskColors = new Color[]
        {
            new Color(0.91f, 0.30f, 0.24f),  // 0: 红色 #E84C3D
            new Color(0.90f, 0.49f, 0.13f),  // 1: 橙色 #E67E22
            new Color(0.95f, 0.77f, 0.06f),  // 2: 黄色 #F1C40F
            new Color(0.18f, 0.80f, 0.44f),  // 3: 绿色 #2ECC71
            new Color(0.20f, 0.60f, 0.86f),  // 4: 蓝色 #3498DB
            new Color(0.61f, 0.35f, 0.71f),  // 5: 紫色 #9B59B6
        };
        
        // 颜色名称
        public static readonly string[] ColorNames = new string[]
        {
            "红色", "橙色", "黄色", "绿色", "蓝色", "紫色"
        };
        
        // UI主题色
        public static class Theme
        {
            // 主色调
            public static readonly Color Primary = new Color(0.91f, 0.30f, 0.24f);      // 番茄红
            public static readonly Color PrimaryDark = new Color(0.75f, 0.22f, 0.17f);
            public static readonly Color PrimaryLight = new Color(0.95f, 0.45f, 0.38f);
            
            // 背景色
            public static readonly Color Background = new Color(0.96f, 0.96f, 0.96f);   // 浅灰背景
            public static readonly Color Surface = Color.white;                          // 卡片背景
            public static readonly Color SurfaceVariant = new Color(0.92f, 0.92f, 0.92f);
            
            // 文字色
            public static readonly Color TextPrimary = new Color(0.13f, 0.13f, 0.13f);  // 主要文字
            public static readonly Color TextSecondary = new Color(0.45f, 0.45f, 0.45f); // 次要文字
            public static readonly Color TextOnPrimary = Color.white;                    // 主色上的文字
            
            // 状态色
            public static readonly Color Success = new Color(0.18f, 0.80f, 0.44f);      // 成功/完成
            public static readonly Color Warning = new Color(0.95f, 0.77f, 0.06f);      // 警告
            public static readonly Color Error = new Color(0.91f, 0.30f, 0.24f);        // 错误
            public static readonly Color Info = new Color(0.20f, 0.60f, 0.86f);         // 信息
            
            // 计时器状态色
            public static readonly Color FocusColor = new Color(0.91f, 0.30f, 0.24f);   // 专注状态
            public static readonly Color BreakColor = new Color(0.18f, 0.80f, 0.44f);   // 休息状态
            public static readonly Color PausedColor = new Color(0.95f, 0.77f, 0.06f);  // 暂停状态
            
            // 柱状图颜色
            public static readonly Color ChartBar = new Color(0.91f, 0.30f, 0.24f, 0.8f);
            public static readonly Color ChartBarHighlight = new Color(0.91f, 0.30f, 0.24f, 1f);
            public static readonly Color ChartGrid = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        }
        
        /// <summary>
        /// 获取任务颜色
        /// </summary>
        public static Color GetTaskColor(int index)
        {
            return TaskColors[Mathf.Clamp(index, 0, TaskColors.Length - 1)];
        }
        
        /// <summary>
        /// 获取颜色名称
        /// </summary>
        public static string GetColorName(int index)
        {
            return ColorNames[Mathf.Clamp(index, 0, ColorNames.Length - 1)];
        }
        
        /// <summary>
        /// 获取半透明版本的颜色
        /// </summary>
        public static Color GetTransparent(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
        
        /// <summary>
        /// 获取更亮的颜色
        /// </summary>
        public static Color GetLighter(Color color, float factor = 0.2f)
        {
            return Color.Lerp(color, Color.white, factor);
        }
        
        /// <summary>
        /// 获取更暗的颜色
        /// </summary>
        public static Color GetDarker(Color color, float factor = 0.2f)
        {
            return Color.Lerp(color, Color.black, factor);
        }
    }
}
