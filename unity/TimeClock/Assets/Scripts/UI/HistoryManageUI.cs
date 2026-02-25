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
    /// 历史记录管理弹窗
    /// 用于选择性删除任务的历史记录
    /// </summary>
    public class HistoryManageUI : MonoBehaviour
    {
        private static HistoryManageUI _instance;
        public static HistoryManageUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<HistoryManageUI>(true);
                    
                    if (_instance == null)
                    {
                        Debug.LogError("[HistoryManageUI] 场景中找不到 HistoryManageUI 组件！请确保已创建并正确设置。");
                    }
                }
                return _instance;
            }
        }
        
        [Header("面板")]
        [SerializeField] private GameObject panel;
        
        [Header("UI元素")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Transform listContent;
        [SerializeField] private GameObject taskItemPrefab;
        
        [Header("按钮")]
        [SerializeField] private Button selectAllButton;
        [SerializeField] private Button deselectAllButton;
        [SerializeField] private Button deleteSelectedButton;
        [SerializeField] private Button closeButton;
        
        [Header("统计显示")]
        [SerializeField] private TextMeshProUGUI selectedCountText;
        
        // 任务数据类
        [Serializable]
        public class TaskHistoryInfo
        {
            public string taskId;
            public string taskName;
            public int colorIndex;
            public int recordCount;
            public float totalSeconds;
            public bool isDeleted; // 任务是否已从任务列表中删除
            public bool isSelected; // 是否被选中删除
        }
        
        private List<TaskHistoryInfo> taskInfoList = new List<TaskHistoryInfo>();
        private Dictionary<string, Toggle> taskToggles = new Dictionary<string, Toggle>();
        private bool showPending = false; // 标记是否有待显示的请求
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                Debug.Log("[HistoryManageUI] 初始化成功");
            }
            else if (_instance != this)
            {
                Debug.LogWarning("[HistoryManageUI] 场景中存在多个 HistoryManageUI，销毁重复的");
                Destroy(gameObject);
                return;
            }
            
            // 检查必要引用
            if (panel == null)
            {
                Debug.LogError("[HistoryManageUI] panel 未设置！");
            }
            if (listContent == null)
            {
                Debug.LogError("[HistoryManageUI] listContent 未设置！");
            }
            if (taskItemPrefab == null)
            {
                Debug.LogError("[HistoryManageUI] taskItemPrefab 未设置！");
            }
            
            BindEvents();
            
            // 【修复】如果没有待显示的请求，才隐藏
            if (!showPending)
            {
                panel?.SetActive(false);
            }
        }
        
        private void BindEvents()
        {
            selectAllButton?.onClick.AddListener(OnSelectAllClicked);
            deselectAllButton?.onClick.AddListener(OnDeselectAllClicked);
            deleteSelectedButton?.onClick.AddListener(OnDeleteSelectedClicked);
            closeButton?.onClick.AddListener(Hide);
        }
        
        /// <summary>
        /// 显示历史记录管理界面
        /// </summary>
        public void Show()
        {
            Debug.Log("[HistoryManageUI] 显示界面");
            
            // 【修复】标记有待显示的请求，防止 Awake 中的 Hide 覆盖
            showPending = true;
            
            RefreshTaskList();
            
            if (panel != null)
            {
                panel.SetActive(true);
                Debug.Log("[HistoryManageUI] panel 已激活");
            }
            else
            {
                Debug.LogError("[HistoryManageUI] panel 为 null，无法显示界面！");
            }
            
            showPending = false;
        }
        
        /// <summary>
        /// 隐藏界面
        /// </summary>
        public void Hide()
        {
            panel?.SetActive(false);
        }
        
        /// <summary>
        /// 刷新任务列表
        /// </summary>
        private void RefreshTaskList()
        {
            // 清空现有列表
            foreach (Transform child in listContent)
            {
                Destroy(child.gameObject);
            }
            taskToggles.Clear();
            taskInfoList.Clear();
            
            // 收集所有有记录的任务信息
            var records = StatisticsManager.Instance?.GetAllRecords();
            if (records == null || records.Count == 0)
            {
                UpdateSelectedCount();
                return;
            }
            
            // 按任务ID分组统计
            var taskGroups = records
                .Where(r => r.pomodoroType == PomodoroType.Focus)
                .GroupBy(r => r.taskId)
                .ToList();
            
            // 获取当前任务列表
            var currentTasks = TaskManager.Instance?.Tasks;
            var currentTaskIds = currentTasks?.Select(t => t.id).ToHashSet() ?? new HashSet<string>();
            
            foreach (var group in taskGroups)
            {
                var firstRecord = group.First();
                var taskInfo = new TaskHistoryInfo
                {
                    taskId = group.Key,
                    taskName = firstRecord.taskName,
                    colorIndex = firstRecord.taskColorIndex,
                    recordCount = group.Count(),
                    totalSeconds = group.Sum(r => r.durationSeconds),
                    isDeleted = !currentTaskIds.Contains(group.Key),
                    isSelected = false
                };
                
                taskInfoList.Add(taskInfo);
                CreateTaskItem(taskInfo);
            }
            
            UpdateSelectedCount();
        }
        
        /// <summary>
        /// 创建任务列表项
        /// </summary>
        private void CreateTaskItem(TaskHistoryInfo info)
        {
            if (taskItemPrefab == null || listContent == null) return;
            
            var itemObj = Instantiate(taskItemPrefab, listContent);
            
            // 获取组件
            var toggle = itemObj.GetComponentInChildren<Toggle>();
            var texts = itemObj.GetComponentsInChildren<TextMeshProUGUI>();
            var colorBar = itemObj.transform.Find("ColorBar")?.GetComponent<Image>();
            
            // 设置颜色条
            if (colorBar != null)
            {
                colorBar.color = ColorPalette.GetTaskColor(info.colorIndex);
            }
            
            // 设置文本
            if (texts.Length >= 2)
            {
                // 任务名（如果已删除，添加标记）
                string displayName = info.taskName;
                if (info.isDeleted)
                {
                    displayName += $" <color=#888888>{Get("UI_Tasks", "history_deleted_marker")}</color>";
                }
                texts[0].text = displayName;
                
                // 统计信息
                string timeStr = StatisticsManager.FormatTime(info.totalSeconds);
                texts[1].text = "🍅 " + GetSmart("UI_Tasks", "history_stats",
                    ("count", info.recordCount), ("time", timeStr));
            }
            
            // 设置Toggle
            if (toggle != null)
            {
                toggle.isOn = info.isSelected;
                toggle.onValueChanged.AddListener((isOn) =>
                {
                    info.isSelected = isOn;
                    UpdateSelectedCount();
                });
                taskToggles[info.taskId] = toggle;
            }
            
            itemObj.SetActive(true);
        }
        
        /// <summary>
        /// 更新选中数量显示
        /// </summary>
        private void UpdateSelectedCount()
        {
            int selectedCount = taskInfoList.Count(t => t.isSelected);
            int totalRecords = taskInfoList.Where(t => t.isSelected).Sum(t => t.recordCount);
            
            if (selectedCountText != null)
            {
                if (selectedCount == 0)
                {
                    selectedCountText.text = Get("UI_Tasks", "history_none_selected");
                }
                else
                {
                    selectedCountText.text = GetSmart("UI_Tasks", "history_selected",
                        ("count", selectedCount), ("total", totalRecords));
                }
            }
            
            // 更新删除按钮状态
            if (deleteSelectedButton != null)
            {
                deleteSelectedButton.interactable = selectedCount > 0;
            }
        }
        
        #region 按钮事件
        
        private void OnSelectAllClicked()
        {
            AudioManager.Instance?.PlayClick();
            foreach (var info in taskInfoList)
            {
                info.isSelected = true;
                if (taskToggles.TryGetValue(info.taskId, out var toggle))
                {
                    toggle.isOn = true;
                }
            }
            UpdateSelectedCount();
        }
        
        private void OnDeselectAllClicked()
        {
            AudioManager.Instance?.PlayClick();
            foreach (var info in taskInfoList)
            {
                info.isSelected = false;
                if (taskToggles.TryGetValue(info.taskId, out var toggle))
                {
                    toggle.isOn = false;
                }
            }
            UpdateSelectedCount();
        }
        
        private void OnDeleteSelectedClicked()
        {
            AudioManager.Instance?.PlayClick();
            
            var selectedTasks = taskInfoList.Where(t => t.isSelected).ToList();
            if (selectedTasks.Count == 0) return;
            
            int totalRecords = selectedTasks.Sum(t => t.recordCount);
            string taskNames = string.Join("、", selectedTasks.Take(3).Select(t => t.taskName));
            if (selectedTasks.Count > 3)
            {
                taskNames += " " + GetSmart("UI_Tasks", "history_etc_tasks",
                    ("count", selectedTasks.Count));
            }

            ConfirmDialog.Instance?.Show(
                Get("UI_Tasks", "history_delete_title"),
                GetSmart("UI_Tasks", "history_delete_message",
                    ("tasks", taskNames), ("total", totalRecords)),
                () => ExecuteDelete(selectedTasks),
                null,
                Get("UI_General", "btn_delete"),
                Get("UI_General", "btn_cancel"),
                true
            );
        }
        
        /// <summary>
        /// 执行删除操作
        /// </summary>
        private void ExecuteDelete(List<TaskHistoryInfo> tasksToDelete)
        {
            var taskIds = tasksToDelete.Select(t => t.taskId).ToList();
            
            // 删除记录
            StatisticsManager.Instance?.DeleteRecordsByTaskIds(taskIds);
            
            // 更新现有任务的统计数据
            foreach (var taskId in taskIds)
            {
                TaskManager.Instance?.ResetTaskStatistics(taskId);
            }
            
            // 保存
            DataManager.Instance?.Save();
            
            // 触发统计更新事件
            StatisticsManager.Instance?.NotifyStatisticsUpdated();
            
            Debug.Log($"已删除 {tasksToDelete.Count} 个任务的历史记录");
            
            // 刷新列表
            RefreshTaskList();
        }
        
        #endregion
    }
}
