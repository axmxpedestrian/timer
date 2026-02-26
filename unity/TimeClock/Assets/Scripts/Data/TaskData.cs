using System;
using UnityEngine;
using static PomodoroTimer.Utils.LocalizedText;

namespace PomodoroTimer.Data
{
    /// <summary>
    /// 任务数据结构
    /// </summary>
    [Serializable]
    public class TaskData
    {
        public string id;                    // 唯一标识符
        public string taskName;              // 任务名称
        public int colorIndex;               // 颜色索引 (0-5)
        public int completedPomodoros;       // 已完成的番茄钟数量
        public float totalFocusTimeSeconds;  // 总专注时间(秒)
        public string createdTimeString;     // 创建时间(字符串格式，用于序列化)
        public bool isCompleted;             // 是否已完成
        
        // 运行时使用的DateTime属性
        public DateTime CreatedTime
        {
            get
            {
                if (string.IsNullOrEmpty(createdTimeString))
                    return DateTime.Now;
                try
                {
                    return DateTime.Parse(createdTimeString);
                }
                catch
                {
                    return DateTime.Now;
                }
            }
            set
            {
                createdTimeString = value.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
        
        /// <summary>
        /// 默认构造函数（用于反序列化）
        /// </summary>
        public TaskData()
        {
            id = "";
            taskName = "";
            colorIndex = 0;
            completedPomodoros = 0;
            totalFocusTimeSeconds = 0;
            createdTimeString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            isCompleted = false;
        }
        
        /// <summary>
        /// 创建新任务
        /// </summary>
        public TaskData(string name, int color = 0)
        {
            id = Guid.NewGuid().ToString();
            taskName = name;
            colorIndex = Mathf.Clamp(color, 0, 5);
            completedPomodoros = 0;
            totalFocusTimeSeconds = 0;
            createdTimeString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            isCompleted = false;
        }
        
        /// <summary>
        /// 添加完成的番茄钟
        /// </summary>
        public void AddCompletedPomodoro(float focusTimeSeconds)
        {
            completedPomodoros++;
            totalFocusTimeSeconds += focusTimeSeconds;
        }
        
        /// <summary>
        /// 获取格式化的总时间字符串
        /// </summary>
        public string GetFormattedTotalTime()
        {
            int hours = (int)(totalFocusTimeSeconds / 3600);
            int minutes = (int)((totalFocusTimeSeconds % 3600) / 60);

            if (hours > 0)
                return GetSmart("UI_General", "time_hours_minutes",
                    ("hours", hours), ("minutes", minutes));
            else
                return GetSmart("UI_General", "time_minutes",
                    ("minutes", minutes));
        }
        
        /// <summary>
        /// 复制任务数据
        /// </summary>
        public TaskData Clone()
        {
            return new TaskData(taskName, colorIndex)
            {
                id = this.id,
                completedPomodoros = this.completedPomodoros,
                totalFocusTimeSeconds = this.totalFocusTimeSeconds,
                createdTimeString = this.createdTimeString,
                isCompleted = this.isCompleted
            };
        }
        
        /// <summary>
        /// 验证任务数据是否有效
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(taskName);
        }
    }
}
