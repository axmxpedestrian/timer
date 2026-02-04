using System;
using System.Collections.Generic;
using UnityEngine;

namespace PomodoroTimer.Data
{
    /// <summary>
    /// 存档数据结构 - 包含所有需要持久化的数据
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // 版本号，用于未来的数据迁移
        public int version = 1;
        
        // 任务列表
        public List<TaskData> tasks = new List<TaskData>();
        
        // 番茄钟记录历史
        public List<PomodoroRecord> pomodoroRecords = new List<PomodoroRecord>();
        
        // 设置
        public SettingsData settings = new SettingsData();
        
        // 当前状态(用于恢复中断的会话)
        public SessionData currentSession = new SessionData();
        
        // 统计数据
        public StatisticsData statistics = new StatisticsData();
        
        // 最后保存时间
        public string lastSaveTime;
        
        public SaveData()
        {
            lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        /// <summary>
        /// 更新保存时间
        /// </summary>
        public void UpdateSaveTime()
        {
            lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
    
    /// <summary>
    /// 设置数据
    /// </summary>
    [Serializable]
    public class SettingsData
    {
        public int focusDurationMinutes = 25;       // 专注时长(分钟)
        public int shortBreakMinutes = 5;           // 短休息时长(分钟)
        public int longBreakMinutes = 30;           // 长休息时长(分钟)
        public int roundsBeforeLongBreak = 4;       // 长休息前的轮数
        public int countupMinThreshold = 10;        // 正计时最小有效时长(分钟)
        public int countupMaxMinutes = 120;         // 正计时最大时长(分钟)
        public bool soundEnabled = true;            // 是否启用声音
        public float soundVolume = 0.8f;            // 音量 (0-1)
        public bool autoStartBreak = false;         // 专注结束后自动开始休息
        public bool autoStartFocus = false;         // 休息结束后自动开始专注
        public bool topMost = false;                // 窗口置顶
    }
    
    /// <summary>
    /// 会话数据 - 用于保存/恢复中断的计时
    /// </summary>
    [Serializable]
    public class SessionData
    {
        public bool hasActiveSession = false;       // 是否有活动会话
        public string currentTaskId = "";           // 当前任务ID
        public int currentRound = 1;                // 当前轮次
        public PomodoroType currentType = PomodoroType.Focus;  // 当前类型
        public TimerMode currentMode = TimerMode.Countdown;    // 当前模式
        public float elapsedSeconds = 0;            // 已经过的秒数
        public string sessionStartTime;             // 会话开始时间
        public long startTimestamp;                 // 开始时间戳（用于真实时间计算）
        
        public void Clear()
        {
            hasActiveSession = false;
            currentTaskId = "";
            currentRound = 1;
            currentType = PomodoroType.Focus;
            currentMode = TimerMode.Countdown;
            elapsedSeconds = 0;
            sessionStartTime = "";
            startTimestamp = 0;
        }
        
        public void StartSession(string taskId, PomodoroType type, TimerMode mode, int round)
        {
            hasActiveSession = true;
            currentTaskId = taskId;
            currentType = type;
            currentMode = mode;
            currentRound = round;
            elapsedSeconds = 0;
            sessionStartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            startTimestamp = DateTime.Now.Ticks;
        }
    }
    
    /// <summary>
    /// 统计数据
    /// </summary>
    [Serializable]
    public class StatisticsData
    {
        public int totalPomodorosCompleted = 0;     // 总完成番茄钟数
        public float totalFocusTimeSeconds = 0;     // 总专注时间(秒)
        public int currentStreak = 0;               // 当前连续天数
        public int longestStreak = 0;               // 最长连续天数
        public string lastActiveDate;               // 最后活跃日期
        public int totalCoins = 0;                  // 总代币数量
        
        /// <summary>
        /// 添加完成的番茄钟
        /// </summary>
        public void AddCompletedPomodoro(float focusTimeSeconds)
        {
            totalPomodorosCompleted++;
            totalFocusTimeSeconds += focusTimeSeconds;
            
            // 计算并添加代币
            int earnedCoins = CalculateCoins(focusTimeSeconds / 60f);
            totalCoins += earnedCoins;
            
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            
            if (lastActiveDate != today)
            {
                // 检查是否连续
                if (!string.IsNullOrEmpty(lastActiveDate))
                {
                    DateTime lastDate = DateTime.Parse(lastActiveDate);
                    if ((DateTime.Now.Date - lastDate).Days == 1)
                    {
                        currentStreak++;
                    }
                    else if ((DateTime.Now.Date - lastDate).Days > 1)
                    {
                        currentStreak = 1;
                    }
                }
                else
                {
                    currentStreak = 1;
                }
                
                if (currentStreak > longestStreak)
                    longestStreak = currentStreak;
                    
                lastActiveDate = today;
            }
        }
        
        /// <summary>
        /// 计算代币数量
        /// 公式: y = 0.5 * (x - 4), 10 ≤ x ≤ 120; y = 60, x ≥ 120
        /// </summary>
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
        /// 获取格式化的总时间
        /// </summary>
        public string GetFormattedTotalTime()
        {
            int hours = (int)(totalFocusTimeSeconds / 3600);
            int minutes = (int)((totalFocusTimeSeconds % 3600) / 60);
            return $"{hours}小时{minutes}分钟";
        }
    }
}
