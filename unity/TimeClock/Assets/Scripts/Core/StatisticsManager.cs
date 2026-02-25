using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PomodoroTimer.Data;
using static PomodoroTimer.Utils.LocalizedText;

namespace PomodoroTimer.Core
{
    /// <summary>
    /// 统计缓存项
    /// </summary>
    public class StatisticsCacheItem<T>
    {
        public T data;
        public DateTime cacheTime;
        public string cacheKey;
        
        public bool IsValid(float maxAgeSeconds = 60f)
        {
            return (DateTime.Now - cacheTime).TotalSeconds < maxAgeSeconds;
        }
    }
    
    /// <summary>
    /// 统计管理器 - 带缓存优化
    /// </summary>
    public class StatisticsManager : MonoBehaviour
    {
        public static StatisticsManager Instance { get; private set; }
        
        private List<PomodoroRecord> records = new List<PomodoroRecord>();
        private StatisticsData statistics;
        
        // 缓存系统
        private Dictionary<string, StatisticsCacheItem<List<DailyStatistics>>> dailyCache = new Dictionary<string, StatisticsCacheItem<List<DailyStatistics>>>();
        private Dictionary<string, StatisticsCacheItem<List<WeeklyStatistics>>> weeklyCache = new Dictionary<string, StatisticsCacheItem<List<WeeklyStatistics>>>();
        private Dictionary<string, StatisticsCacheItem<List<MonthlyStatistics>>> monthlyCache = new Dictionary<string, StatisticsCacheItem<List<MonthlyStatistics>>>();
        private Dictionary<string, StatisticsCacheItem<List<YearlyStatistics>>> yearlyCache = new Dictionary<string, StatisticsCacheItem<List<YearlyStatistics>>>();
        
        [Header("缓存设置")]
        [SerializeField] private float cacheValidSeconds = 30f; // 缓存有效期（秒）
        [SerializeField] private bool enableCache = true;
        [SerializeField] private bool enableDebugLog = false;
        
        private int recordCountAtLastCache = 0; // 上次缓存时的记录数量
        
        public event Action OnStatisticsUpdated;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[StatisticsManager] {message}");
            }
        }
        
        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(List<PomodoroRecord> savedRecords, StatisticsData savedStats)
        {
            records = savedRecords ?? new List<PomodoroRecord>();
            statistics = savedStats ?? new StatisticsData();
            InvalidateAllCache();
        }
        
        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void InvalidateAllCache()
        {
            dailyCache.Clear();
            weeklyCache.Clear();
            monthlyCache.Clear();
            yearlyCache.Clear();
            recordCountAtLastCache = records.Count;
            Log("所有缓存已清除");
        }
        
        /// <summary>
        /// 检查是否需要刷新缓存（记录数量变化）
        /// </summary>
        private bool NeedRefreshCache()
        {
            return records.Count != recordCountAtLastCache;
        }
        
        /// <summary>
        /// 生成缓存键
        /// </summary>
        private string GetCacheKey(string prefix, int count, string taskId)
        {
            return $"{prefix}_{count}_{taskId ?? "all"}";
        }
        
        /// <summary>
        /// 添加完成的番茄钟
        /// </summary>
        public void AddCompletedPomodoro(float focusTimeSeconds, string taskId)
        {
            statistics.AddCompletedPomodoro(focusTimeSeconds);
            InvalidateAllCache(); // 数据变化时清除缓存
            OnStatisticsUpdated?.Invoke();
        }
        
        /// <summary>
        /// 添加记录到列表（供DataManager调用）
        /// </summary>
        public void AddRecord(PomodoroRecord record)
        {
            if (record != null && !records.Contains(record))
            {
                records.Add(record);
                InvalidateAllCache(); // 数据变化时清除缓存
            }
        }
        
        /// <summary>
        /// 获取所有记录
        /// </summary>
        public List<PomodoroRecord> GetAllRecords()
        {
            return new List<PomodoroRecord>(records);
        }
        
        /// <summary>
        /// 获取今日记录
        /// </summary>
        public List<PomodoroRecord> GetTodayRecords()
        {
            var today = DateTime.Now.Date;
            return records.Where(r => r.GetDate() == today && r.pomodoroType == PomodoroType.Focus).ToList();
        }
        
        /// <summary>
        /// 获取指定日期范围的记录
        /// </summary>
        public List<PomodoroRecord> GetRecordsByDateRange(DateTime startDate, DateTime endDate)
        {
            return records.Where(r => 
                r.GetDate() >= startDate.Date && 
                r.GetDate() <= endDate.Date &&
                r.pomodoroType == PomodoroType.Focus
            ).ToList();
        }
        
        /// <summary>
        /// 获取今日统计数据
        /// </summary>
        public DailyStatistics GetTodayStatistics(string taskId = null)
        {
            var todayRecords = GetTodayRecords();
            
            if (!string.IsNullOrEmpty(taskId))
            {
                todayRecords = todayRecords.Where(r => r.taskId == taskId).ToList();
            }
            
            return new DailyStatistics
            {
                date = DateTime.Now.Date,
                pomodoroCount = todayRecords.Count,
                totalFocusSeconds = todayRecords.Sum(r => r.durationSeconds)
            };
        }
        
        /// <summary>
        /// 获取每日统计(用于柱状图) - 带缓存
        /// </summary>
        public List<DailyStatistics> GetDailyStatistics(int days = 7, string taskId = null)
        {
            string cacheKey = GetCacheKey("daily", days, taskId);
            
            // 检查缓存
            if (enableCache && !NeedRefreshCache() && dailyCache.TryGetValue(cacheKey, out var cached))
            {
                if (cached.IsValid(cacheValidSeconds))
                {
                    Log($"使用每日统计缓存: {cacheKey}");
                    return cached.data;
                }
            }
            
            // 计算统计
            Log($"计算每日统计: {cacheKey}");
            var result = CalculateDailyStatistics(days, taskId);
            
            // 存入缓存
            if (enableCache)
            {
                dailyCache[cacheKey] = new StatisticsCacheItem<List<DailyStatistics>>
                {
                    data = result,
                    cacheTime = DateTime.Now,
                    cacheKey = cacheKey
                };
                recordCountAtLastCache = records.Count;
            }
            
            return result;
        }
        
        private List<DailyStatistics> CalculateDailyStatistics(int days, string taskId)
        {
            var result = new List<DailyStatistics>();
            var today = DateTime.Now.Date;
            
            for (int i = days - 1; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var dayRecords = records.Where(r => 
                    r.GetDate() == date && 
                    r.pomodoroType == PomodoroType.Focus &&
                    (string.IsNullOrEmpty(taskId) || r.taskId == taskId)
                ).ToList();
                
                result.Add(new DailyStatistics
                {
                    date = date,
                    pomodoroCount = dayRecords.Count,
                    totalFocusSeconds = dayRecords.Sum(r => r.durationSeconds)
                });
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取每日统计（带任务分解，用于堆叠柱状图）- 带缓存
        /// </summary>
        public List<DailyStatistics> GetDailyStatisticsWithTasks(int days = 7)
        {
            string cacheKey = GetCacheKey("daily_stacked", days, null);
            
            if (enableCache && !NeedRefreshCache() && dailyCache.TryGetValue(cacheKey, out var cached))
            {
                if (cached.IsValid(cacheValidSeconds))
                {
                    Log($"使用每日堆叠统计缓存: {cacheKey}");
                    return cached.data;
                }
            }
            
            Log($"计算每日堆叠统计: {cacheKey}");
            var result = CalculateDailyStatisticsWithTasks(days);
            
            if (enableCache)
            {
                dailyCache[cacheKey] = new StatisticsCacheItem<List<DailyStatistics>>
                {
                    data = result,
                    cacheTime = DateTime.Now,
                    cacheKey = cacheKey
                };
                recordCountAtLastCache = records.Count;
            }
            
            return result;
        }
        
        private List<DailyStatistics> CalculateDailyStatisticsWithTasks(int days)
        {
            var result = new List<DailyStatistics>();
            var today = DateTime.Now.Date;
            
            for (int i = days - 1; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var dayRecords = records.Where(r => 
                    r.GetDate() == date && 
                    r.pomodoroType == PomodoroType.Focus
                ).ToList();
                
                result.Add(new DailyStatistics
                {
                    date = date,
                    pomodoroCount = dayRecords.Count,
                    totalFocusSeconds = dayRecords.Sum(r => r.durationSeconds),
                    taskBreakdown = GetTaskBreakdown(dayRecords)
                });
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取每周统计 - 带缓存
        /// </summary>
        public List<WeeklyStatistics> GetWeeklyStatistics(int weeks = 8, string taskId = null)
        {
            string cacheKey = GetCacheKey("weekly", weeks, taskId);
            
            if (enableCache && !NeedRefreshCache() && weeklyCache.TryGetValue(cacheKey, out var cached))
            {
                if (cached.IsValid(cacheValidSeconds))
                {
                    Log($"使用每周统计缓存: {cacheKey}");
                    return cached.data;
                }
            }
            
            Log($"计算每周统计: {cacheKey}");
            var result = CalculateWeeklyStatistics(weeks, taskId);
            
            if (enableCache)
            {
                weeklyCache[cacheKey] = new StatisticsCacheItem<List<WeeklyStatistics>>
                {
                    data = result,
                    cacheTime = DateTime.Now,
                    cacheKey = cacheKey
                };
                recordCountAtLastCache = records.Count;
            }
            
            return result;
        }
        
        private List<WeeklyStatistics> CalculateWeeklyStatistics(int weeks, string taskId)
        {
            var result = new List<WeeklyStatistics>();
            var today = DateTime.Now.Date;
            
            // 获取本周周一
            int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
            var thisMonday = today.AddDays(-daysFromMonday);
            
            for (int w = weeks - 1; w >= 0; w--)
            {
                var weekStart = thisMonday.AddDays(-w * 7);
                var weekEnd = weekStart.AddDays(6);
                
                var weekRecords = records.Where(r => 
                    r.GetDate() >= weekStart && 
                    r.GetDate() <= weekEnd &&
                    r.pomodoroType == PomodoroType.Focus &&
                    (string.IsNullOrEmpty(taskId) || r.taskId == taskId)
                ).ToList();
                
                result.Add(new WeeklyStatistics
                {
                    weekStartDate = weekStart,
                    weekEndDate = weekEnd,
                    pomodoroCount = weekRecords.Count,
                    totalFocusSeconds = weekRecords.Sum(r => r.durationSeconds),
                    dailyBreakdown = GetDailyBreakdown(weekRecords),
                    taskBreakdown = string.IsNullOrEmpty(taskId) ? GetTaskBreakdown(weekRecords) : null
                });
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取一周内每天的分解数据
        /// </summary>
        private float[] GetDailyBreakdown(List<PomodoroRecord> weekRecords)
        {
            float[] breakdown = new float[7]; // 周一到周日
            
            foreach (var record in weekRecords)
            {
                int dayIndex = ((int)record.GetDate().DayOfWeek + 6) % 7; // 转换为周一=0
                breakdown[dayIndex] += record.durationSeconds;
            }
            
            return breakdown;
        }
        
        /// <summary>
        /// 获取每月统计 - 带缓存
        /// </summary>
        public List<MonthlyStatistics> GetMonthlyStatistics(int months = 12, string taskId = null)
        {
            string cacheKey = GetCacheKey("monthly", months, taskId);
            
            if (enableCache && !NeedRefreshCache() && monthlyCache.TryGetValue(cacheKey, out var cached))
            {
                if (cached.IsValid(cacheValidSeconds))
                {
                    Log($"使用每月统计缓存: {cacheKey}");
                    return cached.data;
                }
            }
            
            Log($"计算每月统计: {cacheKey}");
            var result = CalculateMonthlyStatistics(months, taskId);
            
            if (enableCache)
            {
                monthlyCache[cacheKey] = new StatisticsCacheItem<List<MonthlyStatistics>>
                {
                    data = result,
                    cacheTime = DateTime.Now,
                    cacheKey = cacheKey
                };
                recordCountAtLastCache = records.Count;
            }
            
            return result;
        }
        
        private List<MonthlyStatistics> CalculateMonthlyStatistics(int months, string taskId)
        {
            var result = new List<MonthlyStatistics>();
            var today = DateTime.Now.Date;
            
            for (int m = months - 1; m >= 0; m--)
            {
                var monthDate = today.AddMonths(-m);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                
                var monthRecords = records.Where(r => 
                    r.GetDate() >= monthStart && 
                    r.GetDate() <= monthEnd &&
                    r.pomodoroType == PomodoroType.Focus &&
                    (string.IsNullOrEmpty(taskId) || r.taskId == taskId)
                ).ToList();
                
                result.Add(new MonthlyStatistics
                {
                    year = monthDate.Year,
                    month = monthDate.Month,
                    pomodoroCount = monthRecords.Count,
                    totalFocusSeconds = monthRecords.Sum(r => r.durationSeconds),
                    taskBreakdown = string.IsNullOrEmpty(taskId) ? GetTaskBreakdown(monthRecords) : null
                });
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取每年统计 - 带缓存
        /// </summary>
        public List<YearlyStatistics> GetYearlyStatistics(int years = 8, string taskId = null)
        {
            string cacheKey = GetCacheKey("yearly", years, taskId);
            
            if (enableCache && !NeedRefreshCache() && yearlyCache.TryGetValue(cacheKey, out var cached))
            {
                if (cached.IsValid(cacheValidSeconds))
                {
                    Log($"使用每年统计缓存: {cacheKey}");
                    return cached.data;
                }
            }
            
            Log($"计算每年统计: {cacheKey}");
            var result = CalculateYearlyStatistics(years, taskId);
            
            if (enableCache)
            {
                yearlyCache[cacheKey] = new StatisticsCacheItem<List<YearlyStatistics>>
                {
                    data = result,
                    cacheTime = DateTime.Now,
                    cacheKey = cacheKey
                };
                recordCountAtLastCache = records.Count;
            }
            
            return result;
        }
        
        private List<YearlyStatistics> CalculateYearlyStatistics(int years, string taskId)
        {
            var result = new List<YearlyStatistics>();
            var currentYear = DateTime.Now.Year;
            
            for (int y = years - 1; y >= 0; y--)
            {
                var year = currentYear - y;
                var yearStart = new DateTime(year, 1, 1);
                var yearEnd = new DateTime(year, 12, 31);
                
                var yearRecords = records.Where(r => 
                    r.GetDate() >= yearStart && 
                    r.GetDate() <= yearEnd &&
                    r.pomodoroType == PomodoroType.Focus &&
                    (string.IsNullOrEmpty(taskId) || r.taskId == taskId)
                ).ToList();
                
                result.Add(new YearlyStatistics
                {
                    year = year,
                    pomodoroCount = yearRecords.Count,
                    totalFocusSeconds = yearRecords.Sum(r => r.durationSeconds),
                    taskBreakdown = string.IsNullOrEmpty(taskId) ? GetTaskBreakdown(yearRecords) : null
                });
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取按任务分解的数据（用于堆叠柱状图）
        /// </summary>
        private List<TaskBreakdownItem> GetTaskBreakdown(List<PomodoroRecord> periodRecords)
        {
            if (periodRecords == null || periodRecords.Count == 0)
                return new List<TaskBreakdownItem>();
                
            return periodRecords
                .GroupBy(r => r.taskId)
                .Select(g => new TaskBreakdownItem
                {
                    taskId = g.Key,
                    taskName = g.First().taskName,
                    colorIndex = g.First().taskColorIndex,
                    totalSeconds = g.Sum(r => r.durationSeconds)
                })
                .OrderByDescending(t => t.totalSeconds)
                .ToList();
        }
        
        /// <summary>
        /// 获取任务统计
        /// </summary>
        public TaskStatistics GetTaskStatistics(string taskId)
        {
            var taskRecords = records.Where(r => 
                r.taskId == taskId && 
                r.pomodoroType == PomodoroType.Focus
            ).ToList();
            
            return new TaskStatistics
            {
                taskId = taskId,
                pomodoroCount = taskRecords.Count,
                totalFocusSeconds = taskRecords.Sum(r => r.durationSeconds),
                firstRecordDate = taskRecords.Count > 0 ? taskRecords.Min(r => r.StartTime) : DateTime.Now,
                lastRecordDate = taskRecords.Count > 0 ? taskRecords.Max(r => r.StartTime) : DateTime.Now
            };
        }
        
        /// <summary>
        /// 获取所有任务的统计汇总
        /// </summary>
        public List<TaskStatistics> GetAllTaskStatistics()
        {
            var taskIds = records
                .Where(r => r.pomodoroType == PomodoroType.Focus)
                .Select(r => r.taskId)
                .Distinct();
            
            return taskIds.Select(id => GetTaskStatistics(id)).ToList();
        }
        
        /// <summary>
        /// 获取总体统计
        /// </summary>
        public StatisticsData GetOverallStatistics()
        {
            return statistics;
        }
        
        /// <summary>
        /// 删除指定任务ID的所有记录
        /// </summary>
        public void DeleteRecordsByTaskIds(List<string> taskIds)
        {
            if (taskIds == null || taskIds.Count == 0) return;
            
            var taskIdSet = new HashSet<string>(taskIds);
            
            // 计算要删除的记录统计
            var recordsToDelete = records.Where(r => taskIdSet.Contains(r.taskId)).ToList();
            float totalDeletedSeconds = recordsToDelete
                .Where(r => r.pomodoroType == PomodoroType.Focus)
                .Sum(r => r.durationSeconds);
            int deletedCount = recordsToDelete.Count(r => r.pomodoroType == PomodoroType.Focus);
            
            // 删除记录
            records.RemoveAll(r => taskIdSet.Contains(r.taskId));
            
            // 更新总体统计
            if (statistics != null)
            {
                statistics.totalPomodorosCompleted = Mathf.Max(0, statistics.totalPomodorosCompleted - deletedCount);
                statistics.totalFocusTimeSeconds = Mathf.Max(0, statistics.totalFocusTimeSeconds - totalDeletedSeconds);
            }
            
            // 清除缓存
            InvalidateAllCache();
            
            Debug.Log($"已删除 {deletedCount} 条记录，共 {totalDeletedSeconds / 60f:F1} 分钟");
        }
        
        /// <summary>
        /// 删除指定任务ID的所有记录（单个任务）
        /// </summary>
        public void DeleteRecordsByTaskId(string taskId)
        {
            if (string.IsNullOrEmpty(taskId)) return;
            DeleteRecordsByTaskIds(new List<string> { taskId });
        }
        
        /// <summary>
        /// 通知统计数据已更新
        /// </summary>
        public void NotifyStatisticsUpdated()
        {
            InvalidateAllCache();
            OnStatisticsUpdated?.Invoke();
        }
        
        /// <summary>
        /// 获取缓存状态信息（调试用）
        /// </summary>
        public string GetCacheStatus()
        {
            return $"缓存状态: Daily={dailyCache.Count}, Weekly={weeklyCache.Count}, Monthly={monthlyCache.Count}, Yearly={yearlyCache.Count}";
        }
        
        // 时间段统计缓存（轻量级，只有12个float）
        private float[] hourlyDistributionCache;
        private int hourlyDistributionRecordCount = -1;

        /// <summary>
        /// 获取一天中每2小时时间段的平均专注时间分布
        /// 返回12个时间段的平均分钟数（0-2, 2-4, ..., 22-24）
        /// </summary>
        public float[] GetHourlyDistribution()
        {
            // 检查缓存是否有效
            if (hourlyDistributionCache != null && hourlyDistributionRecordCount == records.Count)
            {
                return hourlyDistributionCache;
            }

            Log("计算时间段分布统计");

            // 统计每个时间段的总分钟数和天数
            float[] totalMinutes = new float[12];  // 12个2小时时间段
            HashSet<DateTime>[] activeDays = new HashSet<DateTime>[12];
            for (int i = 0; i < 12; i++)
            {
                activeDays[i] = new HashSet<DateTime>();
            }

            foreach (var record in records)
            {
                if (record.pomodoroType != PomodoroType.Focus) continue;

                // 获取记录的开始小时
                int hour = record.StartTime.Hour;
                int slotIndex = hour / 2;  // 0-1->0, 2-3->1, ..., 22-23->11

                totalMinutes[slotIndex] += record.durationSeconds / 60f;
                activeDays[slotIndex].Add(record.GetDate());
            }

            // 计算每个时间段的平均值（按有记录的天数平均）
            hourlyDistributionCache = new float[12];
            for (int i = 0; i < 12; i++)
            {
                int dayCount = activeDays[i].Count;
                hourlyDistributionCache[i] = dayCount > 0 ? totalMinutes[i] / dayCount : 0;
            }

            hourlyDistributionRecordCount = records.Count;
            return hourlyDistributionCache;
        }

        /// <summary>
        /// 获取时间段标签（用于图表X轴）
        /// </summary>
        public static string[] GetHourlySlotLabels()
        {
            return new string[]
            {
                "0-2", "2-4", "4-6", "6-8", "8-10", "10-12",
                "12-14", "14-16", "16-18", "18-20", "20-22", "22-24"
            };
        }

        /// <summary>
        /// 格式化时间为小时分钟
        /// </summary>
        public static string FormatTime(float seconds)
        {
            int hours = (int)(seconds / 3600);
            int minutes = (int)((seconds % 3600) / 60);

            if (hours > 0)
                return GetSmart("UI_General", "time_hours_minutes",
                    ("hours", hours), ("minutes", minutes));
            else
                return GetSmart("UI_General", "time_minutes",
                    ("minutes", minutes));
        }
    }
    
    /// <summary>
    /// 每日统计数据
    /// </summary>
    [Serializable]
    public class DailyStatistics
    {
        public DateTime date;
        public int pomodoroCount;
        public float totalFocusSeconds;
        public List<TaskBreakdownItem> taskBreakdown; // 按任务分解（用于堆叠柱状图）
        
        public string GetFormattedTime()
        {
            return StatisticsManager.FormatTime(totalFocusSeconds);
        }
        
        public string GetDateString()
        {
            return date.ToString("MM/dd");
        }
        
        public string GetDayOfWeekString()
        {
            string[] days = { "日", "一", "二", "三", "四", "五", "六" };
            return days[(int)date.DayOfWeek];
        }
    }
    
    /// <summary>
    /// 每周统计数据
    /// </summary>
    [Serializable]
    public class WeeklyStatistics
    {
        public DateTime weekStartDate;
        public DateTime weekEndDate;
        public int pomodoroCount;
        public float totalFocusSeconds;
        public float[] dailyBreakdown; // 每天的时长
        public List<TaskBreakdownItem> taskBreakdown; // 按任务分解（用于堆叠柱状图）
        
        public string GetFormattedTime()
        {
            return StatisticsManager.FormatTime(totalFocusSeconds);
        }
        
        public string GetWeekLabel()
        {
            return $"{weekStartDate:MM/dd}-{weekEndDate:MM/dd}";
        }
    }
    
    /// <summary>
    /// 每月统计数据
    /// </summary>
    [Serializable]
    public class MonthlyStatistics
    {
        public int year;
        public int month;
        public int pomodoroCount;
        public float totalFocusSeconds;
        public List<TaskBreakdownItem> taskBreakdown;
        
        public string GetFormattedTime()
        {
            return StatisticsManager.FormatTime(totalFocusSeconds);
        }
        
        public string GetMonthLabel()
        {
            return $"{year}/{month:D2}";
        }
        
        public string GetShortLabel()
        {
            return $"{month}月";
        }
    }
    
    /// <summary>
    /// 每年统计数据
    /// </summary>
    [Serializable]
    public class YearlyStatistics
    {
        public int year;
        public int pomodoroCount;
        public float totalFocusSeconds;
        public List<TaskBreakdownItem> taskBreakdown;
        
        public string GetFormattedTime()
        {
            return StatisticsManager.FormatTime(totalFocusSeconds);
        }
        
        public string GetYearLabel()
        {
            return $"{year}年";
        }
    }
    
    /// <summary>
    /// 任务分解项（用于堆叠柱状图）
    /// </summary>
    [Serializable]
    public class TaskBreakdownItem
    {
        public string taskId;
        public string taskName;
        public int colorIndex;
        public float totalSeconds;
    }
    
    /// <summary>
    /// 任务统计数据
    /// </summary>
    [Serializable]
    public class TaskStatistics
    {
        public string taskId;
        public int pomodoroCount;
        public float totalFocusSeconds;
        public DateTime firstRecordDate;
        public DateTime lastRecordDate;
        
        public string GetFormattedTime()
        {
            return StatisticsManager.FormatTime(totalFocusSeconds);
        }
    }
}
