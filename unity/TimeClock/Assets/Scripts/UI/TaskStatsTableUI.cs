using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PomodoroTimer.Core;
using PomodoroTimer.Data;
using PomodoroTimer.Utils;
using static PomodoroTimer.Utils.LocalizedText;

namespace PomodoroTimer.UI
{
    /// <summary>
    /// 排序类型
    /// </summary>
    public enum TaskSortType
    {
        ByCount,    // 按完成数量
        ByTime      // 按总时间
    }
    
    /// <summary>
    /// 排序方向
    /// </summary>
    public enum SortDirection
    {
        Descending, // 降序（大到小）
        Ascending   // 升序（小到大）
    }
    
    /// <summary>
    /// 任务统计表格数据项
    /// </summary>
    public class TaskStatsItem
    {
        public string taskId;
        public string taskName;
        public int colorIndex;
        public int completedCount;
        public float totalSeconds;
        public bool isDeleted;
        
        public string GetFormattedTime()
        {
            int hours = (int)(totalSeconds / 3600);
            int minutes = (int)((totalSeconds % 3600) / 60);

            if (hours > 0)
                return GetSmart("UI_General", "time_hours_minutes",
                    ("hours", hours), ("minutes", minutes));
            else
                return GetSmart("UI_General", "time_minutes",
                    ("minutes", minutes));
        }
    }
    
    /// <summary>
    /// 任务统计表格UI
    /// </summary>
    public class TaskStatsTableUI : MonoBehaviour
    {
        [Header("表格容器")]
        [SerializeField] private RectTransform tableContent;
        [SerializeField] private GameObject rowPrefab;
        
        [Header("表头按钮")]
        [SerializeField] private Button sortByNameButton;
        [SerializeField] private Button sortByCountButton;
        [SerializeField] private Button sortByTimeButton;
        
        [Header("表头指示器")]
        [SerializeField] private TextMeshProUGUI countHeaderText;
        [SerializeField] private TextMeshProUGUI timeHeaderText;
        
        [Header("设置")]
        [SerializeField] private float rowHeight = 40f;
        [SerializeField] private bool enableDebugLog = false;
        
        private List<GameObject> rowInstances = new List<GameObject>();
        private List<TaskStatsItem> statsData = new List<TaskStatsItem>();
        private TaskSortType currentSortType = TaskSortType.ByTime;
        private SortDirection currentDirection = SortDirection.Descending;
        
        private void Start()
        {
            BindEvents();
        }
        
        private void OnEnable()
        {
            RefreshData();
        }
        
        private void BindEvents()
        {
            sortByCountButton?.onClick.AddListener(() => SortBy(TaskSortType.ByCount));
            sortByTimeButton?.onClick.AddListener(() => SortBy(TaskSortType.ByTime));
        }
        
        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[TaskStatsTableUI] {message}");
            }
        }
        
        /// <summary>
        /// 刷新数据
        /// </summary>
        public void RefreshData()
        {
            LoadStatsData();
            SortData();
            RenderTable();
            UpdateHeaderIndicators();
        }
        
        /// <summary>
        /// 加载统计数据
        /// </summary>
        private void LoadStatsData()
        {
            statsData.Clear();
            
            if (StatisticsManager.Instance == null) 
            {
                Log("StatisticsManager.Instance 为 null");
                return;
            }
            
            var records = StatisticsManager.Instance.GetAllRecords();
            if (records == null || records.Count == 0) 
            {
                Log("没有找到任何记录");
                return;
            }
            
            Log($"总共找到 {records.Count} 条记录");
            
            // 按任务ID分组统计
            var taskGroups = records
                .Where(r => r.pomodoroType == PomodoroType.Focus)
                .GroupBy(r => r.taskId);
            
            // 获取当前存在的任务列表及其累计时间（作为备用数据源）
            var existingTasks = new HashSet<string>();
            var taskTotalSeconds = new Dictionary<string, float>();
            if (TaskManager.Instance?.Tasks != null)
            {
                foreach (var task in TaskManager.Instance.Tasks)
                {
                    if (task != null && task.IsValid())
                    {
                        existingTasks.Add(task.id);
                        taskTotalSeconds[task.id] = task.totalFocusTimeSeconds;
                    }
                }
            }
            
            foreach (var group in taskGroups)
            {
                var firstRecord = group.First();
                var recordsList = group.ToList();
                
                // 计算总时间
                float totalSecs = recordsList.Sum(r => r.durationSeconds);
                
                // 【修复】如果记录中的 durationSeconds 为0，尝试从 TaskData 获取
                if (totalSecs <= 0 && taskTotalSeconds.TryGetValue(group.Key, out float taskSecs))
                {
                    totalSecs = taskSecs;
                    Log($"任务 {firstRecord.taskName}: 记录时间为0，使用TaskData的累计时间 {taskSecs}秒");
                }
                
                Log($"任务 {firstRecord.taskName}: 完成数={recordsList.Count}, 总时间={totalSecs}秒 ({totalSecs/60f:F1}分钟)");
                
                // 额外调试：打印每条记录的 durationSeconds
                if (enableDebugLog)
                {
                    foreach (var r in recordsList.Take(3)) // 只打印前3条
                    {
                        Log($"  - 记录 {r.id.Substring(0, 8)}: duration={r.durationSeconds}秒");
                    }
                }
                
                statsData.Add(new TaskStatsItem
                {
                    taskId = group.Key,
                    taskName = firstRecord.taskName,
                    colorIndex = firstRecord.taskColorIndex,
                    completedCount = recordsList.Count,
                    totalSeconds = totalSecs,
                    isDeleted = !existingTasks.Contains(group.Key)
                });
            }
            
            Log($"加载了 {statsData.Count} 个任务的统计数据");
        }
        
        /// <summary>
        /// 按指定类型排序
        /// </summary>
        public void SortBy(TaskSortType sortType)
        {
            AudioManager.Instance?.PlayClick();
            
            // 如果点击同一列，切换排序方向
            if (currentSortType == sortType)
            {
                currentDirection = currentDirection == SortDirection.Descending 
                    ? SortDirection.Ascending 
                    : SortDirection.Descending;
            }
            else
            {
                currentSortType = sortType;
                currentDirection = SortDirection.Descending; // 默认降序
            }
            
            SortData();
            RenderTable();
            UpdateHeaderIndicators();
        }
        
        /// <summary>
        /// 排序数据
        /// </summary>
        private void SortData()
        {
            switch (currentSortType)
            {
                case TaskSortType.ByCount:
                    if (currentDirection == SortDirection.Descending)
                        statsData = statsData.OrderByDescending(x => x.completedCount).ToList();
                    else
                        statsData = statsData.OrderBy(x => x.completedCount).ToList();
                    break;
                    
                case TaskSortType.ByTime:
                    if (currentDirection == SortDirection.Descending)
                        statsData = statsData.OrderByDescending(x => x.totalSeconds).ToList();
                    else
                        statsData = statsData.OrderBy(x => x.totalSeconds).ToList();
                    break;
            }
            
            Log($"排序: {currentSortType}, {currentDirection}");
        }
        
        /// <summary>
        /// 渲染表格
        /// </summary>
        private void RenderTable()
        {
            ClearTable();
            
            if (rowPrefab == null || tableContent == null) return;
            
            for (int i = 0; i < statsData.Count; i++)
            {
                var item = statsData[i];
                CreateRow(item, i);
            }
            
            // 更新Content高度
            var contentHeight = statsData.Count * rowHeight;
            tableContent.sizeDelta = new Vector2(tableContent.sizeDelta.x, contentHeight);
        }
        
        /// <summary>
        /// 创建表格行
        /// </summary>
        private void CreateRow(TaskStatsItem item, int index)
        {
            var rowObj = Instantiate(rowPrefab, tableContent);
            rowObj.SetActive(true);
            
            var rect = rowObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.anchoredPosition = new Vector2(0, -index * rowHeight);
                rect.sizeDelta = new Vector2(0, rowHeight);
            }
            
            // 查找并设置子元素
            // 假设行结构: ColorIndicator, TaskNameText, CountText, TimeText
            
            // 颜色指示器
            var colorIndicator = rowObj.transform.Find("ColorIndicator")?.GetComponent<Image>();
            if (colorIndicator != null)
            {
                colorIndicator.color = ColorPalette.GetTaskColor(item.colorIndex);
            }
            
            // 任务名称
            var nameText = rowObj.transform.Find("TaskNameText")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = item.isDeleted
                    ? $"{item.taskName} {Get("UI_Statistics", "task_deleted_suffix")}"
                    : item.taskName;
                nameText.color = item.isDeleted ? ColorPalette.Theme.TextSecondary : ColorPalette.Theme.TextPrimary;
            }
            
            // 完成数量
            var countText = rowObj.transform.Find("CountText")?.GetComponent<TextMeshProUGUI>();
            if (countText != null)
            {
                countText.text = item.completedCount.ToString();
            }
            
            // 总时间
            var timeText = rowObj.transform.Find("TimeText")?.GetComponent<TextMeshProUGUI>();
            if (timeText != null)
            {
                timeText.text = item.GetFormattedTime();
            }
            
            // 交替行背景色
            var bgImage = rowObj.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = index % 2 == 0 
                    ? new Color(0, 0, 0, 0.05f) 
                    : new Color(0, 0, 0, 0);
            }
            
            rowInstances.Add(rowObj);
        }
        
        /// <summary>
        /// 清空表格
        /// </summary>
        private void ClearTable()
        {
            foreach (var row in rowInstances)
            {
                if (row != null) Destroy(row);
            }
            rowInstances.Clear();
        }
        
        /// <summary>
        /// 更新表头指示器
        /// </summary>
        private void UpdateHeaderIndicators()
        {
            string arrow = currentDirection == SortDirection.Descending ? " ▼" : " ▲";
            
            if (countHeaderText != null)
            {
                string countLabel = Get("UI_Statistics", "table_header_count");
                countHeaderText.text = currentSortType == TaskSortType.ByCount
                    ? $"{countLabel}{arrow}"
                    : countLabel;
            }

            if (timeHeaderText != null)
            {
                string timeLabel = Get("UI_Statistics", "table_header_time");
                timeHeaderText.text = currentSortType == TaskSortType.ByTime
                    ? $"{timeLabel}{arrow}"
                    : timeLabel;
            }
        }
        
        /// <summary>
        /// 获取统计数据（供外部使用）
        /// </summary>
        public List<TaskStatsItem> GetStatsData()
        {
            return new List<TaskStatsItem>(statsData);
        }
    }
}
