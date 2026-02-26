using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PomodoroTimer.Core;
using PomodoroTimer.Data;
using PomodoroTimer.Utils;
using UnityEngine.Localization;
using static PomodoroTimer.Utils.LocalizedText;

namespace PomodoroTimer.UI
{
    /// <summary>
    /// 统计时间范围
    /// </summary>
    public enum StatisticsViewType
    {
        Daily,      // 按天（近7天）
        Weekly,     // 按周（近8周）
        Monthly,    // 按月（近12个月）
        Yearly      // 按年（近8年）
    }
    
    /// <summary>
    /// 统计显示模式
    /// </summary>
    public enum StatisticsDisplayMode
    {
        Chart,      // 柱状图
        Table,      // 任务表格
        Habit       // 使用习惯
    }
    
    /// <summary>
    /// 统计界面UI控制器 - 支持堆叠柱状图、任务表格和代币显示
    /// </summary>
    public class StatisticsUI : MonoBehaviour
    {
        [Header("时间范围标签页")]
        [SerializeField] private Button dailyTabButton;
        [SerializeField] private Button weeklyTabButton;
        [SerializeField] private Button monthlyTabButton;
        [SerializeField] private Button yearlyTabButton;
        [SerializeField] private Image dailyTabIndicator;
        [SerializeField] private Image weeklyTabIndicator;
        [SerializeField] private Image monthlyTabIndicator;
        [SerializeField] private Image yearlyTabIndicator;
        
        [Header("显示模式切换")]
        [SerializeField] private Button chartModeButton;
        [SerializeField] private Button tableModeButton;
        [SerializeField] private Button habitModeButton;      // 使用习惯按钮
        [SerializeField] private Image chartModeIndicator;
        [SerializeField] private Image tableModeIndicator;
        [SerializeField] private Image habitModeIndicator;    // 使用习惯指示器
        
        [Header("柱状图")]
        [SerializeField] private GameObject chartContainer;
        [SerializeField] private BarChartUI barChart;
        [SerializeField] private TextMeshProUGUI chartTitleText;
        
        [Header("任务表格")]
        [SerializeField] private GameObject tableContainer;
        [SerializeField] private TaskStatsTableUI taskStatsTable;
        
        [Header("汇总信息")]
        [SerializeField] private TextMeshProUGUI totalPomodorosText;
        [SerializeField] private TextMeshProUGUI totalTimeText;
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private TextMeshProUGUI averageText;
        [SerializeField] private TextMeshProUGUI totalCoinsText;

        [Header("任务筛选")]
        [SerializeField] private TMP_Dropdown taskFilterDropdown;
        
        [Header("按钮")]
        [SerializeField] private Button closeButton;
        
        [Header("调试")]
        [SerializeField] private bool enableDebugLog = false;
        
        private StatisticsViewType currentViewType = StatisticsViewType.Daily;
        private StatisticsDisplayMode currentDisplayMode = StatisticsDisplayMode.Chart;
        private string selectedTaskId = null;
        private bool isInitialized = false;
        private List<string> taskIdList = new List<string>();
        
        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[StatisticsUI] {message}");
            }
        }
        
        private void OnEnable()
        {
            StartCoroutine(DelayedRefresh());
        }
        
        private System.Collections.IEnumerator DelayedRefresh()
        {
            Canvas.ForceUpdateCanvases();
            
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();
            
            SetupTaskDropdown();
            RefreshStatistics();
        }
        
        private void Start()
        {
            BindEvents();
            SetupTaskDropdown();
            SelectTab(StatisticsViewType.Daily);
            SetDisplayMode(StatisticsDisplayMode.Chart);
            isInitialized = true;

            // 订阅语言切换事件，刷新所有动态文本
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLocaleChanged += OnLocaleChanged;
            }
        }

        private void OnDestroy()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLocaleChanged -= OnLocaleChanged;
            }
        }

        private void OnLocaleChanged(Locale newLocale)
        {
            if (!isInitialized) return;

            SetupTaskDropdown();
            UpdateTabVisual();
            RefreshStatistics();
        }
        
        private void BindEvents()
        {
            closeButton?.onClick.AddListener(OnCloseClicked);
            
            // 时间范围标签页
            dailyTabButton?.onClick.AddListener(() => SelectTab(StatisticsViewType.Daily));
            weeklyTabButton?.onClick.AddListener(() => SelectTab(StatisticsViewType.Weekly));
            monthlyTabButton?.onClick.AddListener(() => SelectTab(StatisticsViewType.Monthly));
            yearlyTabButton?.onClick.AddListener(() => SelectTab(StatisticsViewType.Yearly));
            
            // 显示模式切换
            chartModeButton?.onClick.AddListener(() => SetDisplayMode(StatisticsDisplayMode.Chart));
            tableModeButton?.onClick.AddListener(() => SetDisplayMode(StatisticsDisplayMode.Table));
            habitModeButton?.onClick.AddListener(() => SetDisplayMode(StatisticsDisplayMode.Habit));

            taskFilterDropdown?.onValueChanged.AddListener(OnTaskFilterChanged);
        }
        
        private void SetupTaskDropdown()
        {
            if (taskFilterDropdown == null) return;
            
            taskFilterDropdown.ClearOptions();
            
            var options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData(Get("UI_Statistics", "filter_all_tasks"))
            };
            
            var allTaskIds = new HashSet<string>();
            var taskNames = new Dictionary<string, string>();
            
            var tasks = TaskManager.Instance?.Tasks;
            if (tasks != null)
            {
                foreach (var task in tasks)
                {
                    if (task != null && task.IsValid())
                    {
                        allTaskIds.Add(task.id);
                        taskNames[task.id] = task.taskName;
                    }
                }
            }
            
            var records = StatisticsManager.Instance?.GetAllRecords();
            if (records != null)
            {
                foreach (var record in records)
                {
                    if (!string.IsNullOrEmpty(record.taskId) && !allTaskIds.Contains(record.taskId))
                    {
                        allTaskIds.Add(record.taskId);
                        taskNames[record.taskId] = record.taskName + " " + Get("UI_Statistics", "task_deleted_suffix");
                    }
                }
            }
            
            taskIdList.Clear();
            taskIdList.Add(null);
            
            foreach (var taskId in allTaskIds)
            {
                if (taskNames.TryGetValue(taskId, out var name))
                {
                    options.Add(new TMP_Dropdown.OptionData(name));
                    taskIdList.Add(taskId);
                }
            }
            
            taskFilterDropdown.AddOptions(options);
        }
        
        /// <summary>
        /// 设置显示模式（图表/表格/习惯）
        /// </summary>
        private void SetDisplayMode(StatisticsDisplayMode mode)
        {
            AudioManager.Instance?.PlayClick();
            currentDisplayMode = mode;

            // 更新模式指示器
            if (chartModeIndicator != null)
                chartModeIndicator.enabled = (mode == StatisticsDisplayMode.Chart);
            if (tableModeIndicator != null)
                tableModeIndicator.enabled = (mode == StatisticsDisplayMode.Table);
            if (habitModeIndicator != null)
                habitModeIndicator.enabled = (mode == StatisticsDisplayMode.Habit);

            // 切换显示容器
            chartContainer?.SetActive(mode == StatisticsDisplayMode.Chart || mode == StatisticsDisplayMode.Habit);
            tableContainer?.SetActive(mode == StatisticsDisplayMode.Table);

            // 表格和习惯模式下隐藏时间范围标签页和任务筛选
            bool showTimeFilters = (mode == StatisticsDisplayMode.Chart);
            dailyTabButton?.gameObject.SetActive(showTimeFilters);
            weeklyTabButton?.gameObject.SetActive(showTimeFilters);
            monthlyTabButton?.gameObject.SetActive(showTimeFilters);
            yearlyTabButton?.gameObject.SetActive(showTimeFilters);
            taskFilterDropdown?.gameObject.SetActive(showTimeFilters);

            // 刷新对应视图
            if (mode == StatisticsDisplayMode.Table)
            {
                taskStatsTable?.RefreshData();
            }
            else if (mode == StatisticsDisplayMode.Habit)
            {
                RefreshHabitChart();
            }
            else
            {
                RefreshChart();
            }

            Log($"切换显示模式: {mode}");
        }
        
        private void SelectTab(StatisticsViewType viewType)
        {
            AudioManager.Instance?.PlayClick();
            currentViewType = viewType;
            
            UpdateTabVisual();
            RefreshChart();
        }
        
        private void UpdateTabVisual()
        {
            // 更新标签页指示器
            if (dailyTabIndicator != null)
                dailyTabIndicator.enabled = (currentViewType == StatisticsViewType.Daily);
            if (weeklyTabIndicator != null)
                weeklyTabIndicator.enabled = (currentViewType == StatisticsViewType.Weekly);
            if (monthlyTabIndicator != null)
                monthlyTabIndicator.enabled = (currentViewType == StatisticsViewType.Monthly);
            if (yearlyTabIndicator != null)
                yearlyTabIndicator.enabled = (currentViewType == StatisticsViewType.Yearly);
            
            // 更新标题
            if (chartTitleText != null)
            {
                switch (currentViewType)
                {
                    case StatisticsViewType.Daily:
                        chartTitleText.text = Get("UI_Statistics", "chart_title_daily");
                        break;
                    case StatisticsViewType.Weekly:
                        chartTitleText.text = Get("UI_Statistics", "chart_title_weekly");
                        break;
                    case StatisticsViewType.Monthly:
                        chartTitleText.text = Get("UI_Statistics", "chart_title_monthly");
                        break;
                    case StatisticsViewType.Yearly:
                        chartTitleText.text = Get("UI_Statistics", "chart_title_yearly");
                        break;
                }
            }
        }
        
        private void OnTaskFilterChanged(int index)
        {
            if (index >= 0 && index < taskIdList.Count)
            {
                selectedTaskId = taskIdList[index];
            }
            else
            {
                selectedTaskId = null;
            }
            
            RefreshChart();
        }
        
        public void RefreshStatistics()
        {
            if (StatisticsManager.Instance == null) return;

            Log($"刷新统计, viewType={currentViewType}, mode={currentDisplayMode}");

            if (currentDisplayMode == StatisticsDisplayMode.Chart)
            {
                RefreshChart();
            }
            else if (currentDisplayMode == StatisticsDisplayMode.Habit)
            {
                RefreshHabitChart();
            }
            else
            {
                taskStatsTable?.RefreshData();
            }

            RefreshSummary();
        }
        
        private void RefreshChart()
        {
            if (currentDisplayMode != StatisticsDisplayMode.Chart) return;
            
            switch (currentViewType)
            {
                case StatisticsViewType.Daily:
                    RefreshDailyChart();
                    break;
                case StatisticsViewType.Weekly:
                    RefreshWeeklyChart();
                    break;
                case StatisticsViewType.Monthly:
                    RefreshMonthlyChart();
                    break;
                case StatisticsViewType.Yearly:
                    RefreshYearlyChart();
                    break;
            }
        }
        
        private void RefreshDailyChart()
        {
            if (barChart == null) return;
            
            if (string.IsNullOrEmpty(selectedTaskId))
            {
                var dailyStats = StatisticsManager.Instance.GetDailyStatisticsWithTasks(7);
                
                var labels = new List<string>();
                var stackedData = new List<List<TaskBreakdownItem>>();
                
                foreach (var stat in dailyStats)
                {
                    labels.Add(stat.GetDateString() + "\n" + stat.GetDayOfWeekString());
                    stackedData.Add(stat.taskBreakdown ?? new List<TaskBreakdownItem>());
                }
                
                barChart.SetStackedData(labels, stackedData, Get("UI_General", "unit_minutes"));
            }
            else
            {
                var dailyStats = StatisticsManager.Instance.GetDailyStatistics(7, selectedTaskId);
                
                var labels = new List<string>();
                var values = new List<float>();
                
                foreach (var stat in dailyStats)
                {
                    labels.Add(stat.GetDateString() + "\n" + stat.GetDayOfWeekString());
                    values.Add(stat.totalFocusSeconds / 60f);
                }
                
                barChart.SetData(labels, values, Get("UI_General", "unit_minutes"));
            }
        }
        
        private void RefreshWeeklyChart()
        {
            if (barChart == null) return;
            
            if (string.IsNullOrEmpty(selectedTaskId))
            {
                var weeklyStats = StatisticsManager.Instance.GetWeeklyStatistics(8, null);
                
                var labels = new List<string>();
                var stackedData = new List<List<TaskBreakdownItem>>();
                
                foreach (var stat in weeklyStats)
                {
                    labels.Add(stat.GetWeekLabel());
                    stackedData.Add(stat.taskBreakdown ?? new List<TaskBreakdownItem>());
                }
                
                barChart.SetStackedData(labels, stackedData, Get("UI_General", "unit_minutes"));
            }
            else
            {
                var weeklyStats = StatisticsManager.Instance.GetWeeklyStatistics(8, selectedTaskId);
                
                var labels = new List<string>();
                var values = new List<float>();
                
                foreach (var stat in weeklyStats)
                {
                    labels.Add(stat.GetWeekLabel());
                    values.Add(stat.totalFocusSeconds / 60f);
                }
                
                barChart.SetData(labels, values, Get("UI_General", "unit_minutes"));
            }
        }
        
        private void RefreshMonthlyChart()
        {
            if (barChart == null) return;
            
            if (string.IsNullOrEmpty(selectedTaskId))
            {
                var monthlyStats = StatisticsManager.Instance.GetMonthlyStatistics(12, null);
                
                var labels = new List<string>();
                var stackedData = new List<List<TaskBreakdownItem>>();
                
                foreach (var stat in monthlyStats)
                {
                    labels.Add(stat.GetShortLabel());
                    stackedData.Add(stat.taskBreakdown ?? new List<TaskBreakdownItem>());
                }
                
                barChart.SetStackedData(labels, stackedData, Get("UI_General", "unit_minutes"));
            }
            else
            {
                var monthlyStats = StatisticsManager.Instance.GetMonthlyStatistics(12, selectedTaskId);
                
                var labels = new List<string>();
                var values = new List<float>();
                
                foreach (var stat in monthlyStats)
                {
                    labels.Add(stat.GetShortLabel());
                    values.Add(stat.totalFocusSeconds / 60f);
                }
                
                barChart.SetData(labels, values, Get("UI_General", "unit_minutes"));
            }
        }
        
        private void RefreshYearlyChart()
        {
            if (barChart == null) return;

            if (string.IsNullOrEmpty(selectedTaskId))
            {
                var yearlyStats = StatisticsManager.Instance.GetYearlyStatistics(8, null);

                var labels = new List<string>();
                var stackedData = new List<List<TaskBreakdownItem>>();

                foreach (var stat in yearlyStats)
                {
                    labels.Add(stat.GetYearLabel());
                    stackedData.Add(stat.taskBreakdown ?? new List<TaskBreakdownItem>());
                }

                barChart.SetStackedData(labels, stackedData, Get("UI_General", "unit_minutes"));
            }
            else
            {
                var yearlyStats = StatisticsManager.Instance.GetYearlyStatistics(8, selectedTaskId);

                var labels = new List<string>();
                var values = new List<float>();

                foreach (var stat in yearlyStats)
                {
                    labels.Add(stat.GetYearLabel());
                    values.Add(stat.totalFocusSeconds / 60f);
                }

                barChart.SetData(labels, values, Get("UI_General", "unit_minutes"));
            }
        }

        /// <summary>
        /// 刷新使用习惯图表（每2小时时间段分布）
        /// </summary>
        private void RefreshHabitChart()
        {
            if (barChart == null) return;

            // 更新标题
            if (chartTitleText != null)
            {
                chartTitleText.text = Get("UI_Statistics", "chart_title_habit");
            }

            var distribution = StatisticsManager.Instance.GetHourlyDistribution();
            var slotLabels = StatisticsManager.GetHourlySlotLabels();

            var labels = new List<string>(slotLabels);
            var values = new List<float>(distribution);

            barChart.SetData(labels, values, Get("UI_General", "unit_minutes"));
        }
        
        private void RefreshSummary()
        {
            var overallStats = StatisticsManager.Instance.GetOverallStatistics();
            
            if (overallStats == null) return;
            
            if (totalPomodorosText != null)
            {
                totalPomodorosText.text = $"<sprite name=\"tomato\"> {overallStats.totalPomodorosCompleted}";
            }
            
            if (totalTimeText != null)
            {
                totalTimeText.text = overallStats.GetFormattedTotalTime();
            }
            
            if (streakText != null)
            {
                streakText.text = "<sprite name=\"inspiration\"> " + GetSmart("UI_Statistics", "streak_days",
                    ("days", overallStats.currentStreak));
            }
            
            if (totalCoinsText != null)
            {
                totalCoinsText.text = $"<sprite name=\"coin\"> {overallStats.totalCoins}";
            }
            
            if (averageText != null)
            {
                var dailyStats = StatisticsManager.Instance.GetDailyStatistics(7, selectedTaskId);
                float totalMinutes = 0;
                int activeDays = 0;
                
                foreach (var stat in dailyStats)
                {
                    if (stat.pomodoroCount > 0)
                    {
                        totalMinutes += stat.totalFocusSeconds / 60f;
                        activeDays++;
                    }
                }
                
                float average = activeDays > 0 ? totalMinutes / activeDays : 0;
                averageText.text = GetSmart("UI_Statistics", "daily_average",
                    ("minutes", $"{average:F0}"));
            }
        }
        
        private void OnCloseClicked()
        {
            AudioManager.Instance?.PlayClick();
            if (transform.parent != null)
            {
                transform.parent.gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
