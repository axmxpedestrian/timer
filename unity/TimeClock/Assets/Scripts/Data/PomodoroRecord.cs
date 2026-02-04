using System;

namespace PomodoroTimer.Data
{
    /// <summary>
    /// 单个番茄钟记录
    /// </summary>
    [Serializable]
    public class PomodoroRecord
    {
        public string id;                    // 记录唯一ID
        public string taskId;                // 关联的任务ID
        public string taskName;              // 任务名称(冗余存储，方便查询)
        public int taskColorIndex;           // 任务颜色
        public float durationSeconds;        // 持续时间(秒)
        public string startTimeString;       // 开始时间(字符串格式)
        public string endTimeString;         // 结束时间(字符串格式)
        public PomodoroType pomodoroType;    // 类型(专注/休息)
        public TimerMode timerMode;          // 计时模式(倒计时/正计时)
        public bool isCompleted;             // 是否正常完成(非中断)
        
        // 运行时使用的DateTime属性
        public DateTime StartTime
        {
            get
            {
                if (string.IsNullOrEmpty(startTimeString))
                    return DateTime.Now;
                try
                {
                    return DateTime.Parse(startTimeString);
                }
                catch
                {
                    return DateTime.Now;
                }
            }
            set
            {
                startTimeString = value.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
        
        public DateTime EndTime
        {
            get
            {
                if (string.IsNullOrEmpty(endTimeString))
                    return DateTime.Now;
                try
                {
                    return DateTime.Parse(endTimeString);
                }
                catch
                {
                    return DateTime.Now;
                }
            }
            set
            {
                endTimeString = value.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
        
        /// <summary>
        /// 默认构造函数（用于反序列化）
        /// </summary>
        public PomodoroRecord()
        {
            id = "";
            taskId = "";
            taskName = "";
            taskColorIndex = 0;
            durationSeconds = 0;
            startTimeString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            endTimeString = "";
            pomodoroType = PomodoroType.Focus;
            timerMode = TimerMode.Countdown;
            isCompleted = false;
        }
        
        /// <summary>
        /// 创建新记录
        /// </summary>
        public PomodoroRecord(string taskId, string taskName, int colorIndex, 
                              PomodoroType type, TimerMode mode)
        {
            this.id = Guid.NewGuid().ToString();
            this.taskId = taskId;
            this.taskName = taskName;
            this.taskColorIndex = colorIndex;
            this.pomodoroType = type;
            this.timerMode = mode;
            this.startTimeString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.endTimeString = "";
            this.durationSeconds = 0;
            this.isCompleted = false;
        }
        
        /// <summary>
        /// 完成记录
        /// </summary>
        public void Complete(float duration, bool completed = true)
        {
            this.durationSeconds = duration;
            this.endTimeString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.isCompleted = completed;
        }
        
        /// <summary>
        /// 获取记录日期(仅日期部分)
        /// 【修复】使用结束时间，确保跨天任务计入完成当天
        /// </summary>
        public DateTime GetDate()
        {
            // 如果有结束时间，使用结束时间的日期
            if (!string.IsNullOrEmpty(endTimeString))
            {
                return EndTime.Date;
            }
            // 否则使用开始时间（兼容旧数据）
            return StartTime.Date;
        }
        
        /// <summary>
        /// 获取周几 (0=周日, 1=周一, ... 6=周六)
        /// 【修复】使用结束时间
        /// </summary>
        public int GetDayOfWeek()
        {
            return (int)GetDate().DayOfWeek;
        }
        
        /// <summary>
        /// 获取格式化时长
        /// </summary>
        public string GetFormattedDuration()
        {
            int minutes = (int)(durationSeconds / 60);
            int seconds = (int)(durationSeconds % 60);
            return $"{minutes}:{seconds:D2}";
        }
        
        /// <summary>
        /// 验证记录是否有效
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(taskId);
        }
    }
    
    /// <summary>
    /// 番茄钟类型
    /// </summary>
    [Serializable]
    public enum PomodoroType
    {
        Focus,          // 专注
        ShortBreak,     // 短休息
        LongBreak       // 长休息
    }
    
    /// <summary>
    /// 计时模式
    /// </summary>
    [Serializable]
    public enum TimerMode
    {
        Countdown,      // 倒计时
        Countup         // 正计时
    }
    
    /// <summary>
    /// 计时器状态
    /// </summary>
    [Serializable]
    public enum TimerState
    {
        Idle,           // 空闲
        Running,        // 运行中
        Paused          // 已暂停
    }
}
