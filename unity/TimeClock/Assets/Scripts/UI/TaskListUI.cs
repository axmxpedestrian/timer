using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PomodoroTimer.Core;
using PomodoroTimer.Data;
using static PomodoroTimer.Utils.LocalizedText;

// 解决命名空间冲突
using PomodoroTimerCore = PomodoroTimer.Core.PomodoroTimer;

namespace PomodoroTimer.UI
{
    /// <summary>
    /// 任务列表UI控制器
    /// </summary>
    public class TaskListUI : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private Transform taskListContent;
        [SerializeField] private GameObject taskItemPrefab;
        [SerializeField] private Button addTaskButton;
        [SerializeField] private GameObject taskEditPanel;

        [Header("编辑面板")]
        [SerializeField] private TMP_InputField taskNameInput;
        [SerializeField] private Button[] colorButtons;  // 6个颜色按钮
        [SerializeField] private Button saveTaskButton;
        [SerializeField] private Button deleteTaskButton;
        [SerializeField] private Button cancelEditButton;

        [Header("最小化功能")]
        [SerializeField] private Button minimizeButton;           // 最小化按钮（在TaskListHeader中）
        [SerializeField] private GameObject taskListPanel;        // 任务列表主面板（需要隐藏的部分）
        [SerializeField] private GameObject minimizedTab;         // 最小化后显示的标签（在右侧）
        [SerializeField] private Button restoreButton;            // 恢复按钮（在minimizedTab中）

        [Header("可拖动功能")]
        [SerializeField] private DraggablePanel draggablePanel;   // 拖动组件（在TaskListHeader上）

        [Header("主界面引用")]
        [SerializeField] private MainUIController mainUI;

        [Header("调试")]
        [SerializeField] private bool enableDebugLog = false;

        private bool isMinimized = false;
        private Dictionary<string, TaskItemUI> taskItemMap = new Dictionary<string, TaskItemUI>();
        private TaskData editingTask;
        private int selectedColorIndex = 0;
        private bool isEditMode = false;  // true=编辑现有, false=创建新的
        private bool isInitialized = false;
        private string currentSelectedTaskId = null; // 当前选中的任务ID
        
        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[TaskListUI] {message}");
            }
        }
        
        private void Start()
        {
            BindEvents();

            // 初始化最小化状态
            InitializeMinimizeState();

            // 延迟初始化以确保数据管理器已加载
            StartCoroutine(DelayedInitialize());
        }

        /// <summary>
        /// 初始化最小化状态
        /// </summary>
        private void InitializeMinimizeState()
        {
            // 确保初始状态正确
            isMinimized = false;

            if (taskListPanel != null)
            {
                taskListPanel.SetActive(true);
            }

            if (minimizedTab != null)
            {
                minimizedTab.SetActive(false);
            }

            // 重置拖动面板到默认位置
            if (draggablePanel != null)
            {
                draggablePanel.ResetToDefaultPosition();
            }
        }
        
        private System.Collections.IEnumerator DelayedInitialize()
        {
            // 等待几帧确保所有管理器都已初始化
            yield return null;
            yield return null;
            
            RefreshTaskList();
            isInitialized = true;
        }
        
        private void OnEnable()
        {
            // 每次启用时刷新列表
            if (isInitialized)
            {
                RefreshTaskList();
            }
        }
        
        private void OnDestroy()
        {
            UnbindEvents();
        }
        
        /// <summary>
        /// 绑定事件
        /// </summary>
        private void BindEvents()
        {
            addTaskButton?.onClick.AddListener(OnAddTaskClicked);
            saveTaskButton?.onClick.AddListener(OnSaveTaskClicked);
            deleteTaskButton?.onClick.AddListener(OnDeleteTaskClicked);
            cancelEditButton?.onClick.AddListener(OnCancelEditClicked);

            // 绑定最小化/恢复按钮
            minimizeButton?.onClick.AddListener(OnMinimizeClicked);
            restoreButton?.onClick.AddListener(OnRestoreClicked);

            // 绑定颜色按钮
            for (int i = 0; i < colorButtons.Length; i++)
            {
                int index = i;
                colorButtons[i]?.onClick.AddListener(() => OnColorSelected(index));
            }

            // 订阅数据管理器事件
            if (DataManager.Instance != null)
            {
                DataManager.Instance.OnDataLoaded += OnDataLoaded;
            }

            // 订阅任务管理器事件
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.OnTaskAdded += OnTaskAdded;
                TaskManager.Instance.OnTaskUpdated += OnTaskUpdated;
                TaskManager.Instance.OnTaskDeleted += OnTaskDeleted;
                TaskManager.Instance.OnTaskListChanged += OnTaskListChanged;
            }
        }
        
        /// <summary>
        /// 取消绑定事件
        /// </summary>
        private void UnbindEvents()
        {
            if (DataManager.Instance != null)
            {
                DataManager.Instance.OnDataLoaded -= OnDataLoaded;
            }
            
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.OnTaskAdded -= OnTaskAdded;
                TaskManager.Instance.OnTaskUpdated -= OnTaskUpdated;
                TaskManager.Instance.OnTaskDeleted -= OnTaskDeleted;
                TaskManager.Instance.OnTaskListChanged -= OnTaskListChanged;
            }
        }
        
        /// <summary>
        /// 数据加载完成回调
        /// </summary>
        private void OnDataLoaded()
        {
            Log(" 数据加载完成，刷新任务列表");
            RefreshTaskList();
        }
        
        /// <summary>
        /// 任务列表变化回调
        /// </summary>
        private void OnTaskListChanged()
        {
            Log(" 任务列表变化，刷新显示");
            RefreshTaskList();
        }
        
        /// <summary>
        /// 刷新任务列表
        /// </summary>
        public void RefreshTaskList()
        {
            Log(" 开始刷新任务列表");
            
            // 清除现有项
            foreach (Transform child in taskListContent)
            {
                Destroy(child.gameObject);
            }
            taskItemMap.Clear();
            
            // 创建任务项
            var tasks = TaskManager.Instance?.Tasks;
            if (tasks == null)
            {
                Log(" TaskManager.Instance 或 Tasks 为 null");
                return;
            }
            
            Log($" 找到 {tasks.Count} 个任务");
            
            foreach (var task in tasks)
            {
                if (task != null && task.IsValid())
                {
                    Log($" 创建任务项 - {task.taskName}");
                    CreateTaskItem(task);
                }
            }
            
            // 【修复】恢复选中状态
            RestoreSelectedState();
        }
        
        /// <summary>
        /// 恢复选中状态
        /// </summary>
        private void RestoreSelectedState()
        {
            if (string.IsNullOrEmpty(currentSelectedTaskId))
            {
                return;
            }
            
            // 检查选中的任务是否还存在
            if (taskItemMap.TryGetValue(currentSelectedTaskId, out var itemUI))
            {
                itemUI.SetSelected(true);
                Log($" 恢复选中状态: {currentSelectedTaskId}");
            }
            else
            {
                // 任务已被删除，清除选中状态
                currentSelectedTaskId = null;
                mainUI?.OnTaskDeselected();
                Log(" 选中的任务已不存在，清除选中状态");
            }
        }
        
        /// <summary>
        /// 创建任务项UI
        /// </summary>
        private void CreateTaskItem(TaskData task)
        {
            if (taskItemPrefab == null)
            {
                Debug.LogError("TaskListUI: taskItemPrefab 为 null");
                return;
            }
            
            if (taskListContent == null)
            {
                Debug.LogError("TaskListUI: taskListContent 为 null");
                return;
            }
            
            var itemObj = Instantiate(taskItemPrefab, taskListContent);
            var itemUI = itemObj.GetComponent<TaskItemUI>();
            
            if (itemUI != null)
            {
                itemUI.Setup(task, OnTaskItemSelected, OnTaskItemEdit);
                taskItemMap[task.id] = itemUI;
                Log($" 成功创建任务项 - {task.taskName}");
            }
            else
            {
                Debug.LogError("TaskListUI: TaskItemUI 组件未找到");
            }
        }
        
        #region 按钮事件处理
        
        private void OnAddTaskClicked()
        {
            isEditMode = false;
            editingTask = null;
            selectedColorIndex = 0;

            // 清空输入
            if (taskNameInput != null)
            {
                taskNameInput.text = "";
            }

            // 隐藏删除按钮
            deleteTaskButton?.gameObject.SetActive(false);

            // 更新颜色选择
            UpdateColorSelection();

            // 新建任务时颜色按钮始终可用
            SetColorButtonsInteractable(true);

            // 显示编辑面板
            taskEditPanel?.SetActive(true);
        }
        
        private void OnSaveTaskClicked()
        {
            string taskName = taskNameInput?.text?.Trim();
            
            if (string.IsNullOrEmpty(taskName))
            {
                Debug.LogWarning("任务名称不能为空");
                return;
            }
            
            if (isEditMode && editingTask != null)
            {
                // 更新现有任务
                TaskManager.Instance.UpdateTask(editingTask.id, taskName, selectedColorIndex);
            }
            else
            {
                // 创建新任务
                TaskManager.Instance.CreateTask(taskName, selectedColorIndex);
            }
            
            // 关闭编辑面板
            taskEditPanel?.SetActive(false);
        }
        
        private void OnDeleteTaskClicked()
        {
            Debug.Log("[TaskListUI] 点击删除按钮");
            
            if (editingTask == null)
            {
                Debug.LogWarning("[TaskListUI] editingTask 为 null，无法删除");
                return;
            }
            
            // 【新增】检查任务是否正在运行
            var timer = PomodoroTimerCore.Instance;
            if (timer != null && 
                timer.CurrentState != TimerState.Idle && 
                timer.CurrentTask != null && 
                timer.CurrentTask.id == editingTask.id)
            {
                Debug.Log("[TaskListUI] 任务正在运行中，无法删除");
                
                // 显示提示弹窗
                ConfirmDialog.Instance?.ShowAlert(
                    Get("UI_Tasks", "task_cannot_delete"),
                    Get("UI_Tasks", "task_cannot_delete_msg")
                );
                return;
            }
            
            Debug.Log($"[TaskListUI] 准备删除任务: {editingTask.taskName}");
            
            // 检查 ConfirmDialog 是否存在
            if (ConfirmDialog.Instance == null)
            {
                Debug.LogError("[TaskListUI] ConfirmDialog.Instance 为 null！请检查场景中是否已创建 ConfirmDialog 组件。");
                return;
            }
            
            // 显示确认弹窗
            ConfirmDialog.Instance.ShowDelete(editingTask.taskName, () =>
            {
                Debug.Log($"[TaskListUI] 确认删除任务: {editingTask.taskName}");
                
                // 如果删除的是当前选中的任务，取消选择
                if (currentSelectedTaskId == editingTask.id)
                {
                    currentSelectedTaskId = null;
                    mainUI?.OnTaskDeselected();
                }
                
                TaskManager.Instance.DeleteTask(editingTask.id);
                
                // 关闭编辑面板
                taskEditPanel?.SetActive(false);
            });
        }
        
        private void OnCancelEditClicked()
        {
            taskEditPanel?.SetActive(false);
        }

        /// <summary>
        /// 最小化按钮点击
        /// </summary>
        private void OnMinimizeClicked()
        {
            Log("点击最小化按钮");
            SetMinimized(true);
        }

        /// <summary>
        /// 恢复按钮点击
        /// </summary>
        private void OnRestoreClicked()
        {
            Log("点击恢复按钮");
            SetMinimized(false);
        }

        /// <summary>
        /// 设置最小化状态
        /// </summary>
        public void SetMinimized(bool minimized)
        {
            isMinimized = minimized;

            if (taskListPanel != null)
            {
                taskListPanel.SetActive(!minimized);
            }

            if (minimizedTab != null)
            {
                minimizedTab.SetActive(minimized);
            }

            Log($"任务列表最小化状态: {minimized}");
        }

        /// <summary>
        /// 获取当前是否最小化
        /// </summary>
        public bool IsMinimized => isMinimized;

        /// <summary>
        /// 切换最小化状态
        /// </summary>
        public void ToggleMinimized()
        {
            SetMinimized(!isMinimized);
        }

        /// <summary>
        /// 重置面板到默认位置
        /// </summary>
        public void ResetToDefaultPosition()
        {
            if (draggablePanel != null)
            {
                draggablePanel.ResetToDefaultPosition();
            }
        }

        private void OnColorSelected(int index)
        {
            selectedColorIndex = index;
            UpdateColorSelection();
        }
        
        #endregion
        
        #region 任务项回调

        /// <summary>
        /// 任务项被选中(绑定到计时器)
        /// </summary>
        private void OnTaskItemSelected(TaskData task)
        {
            Log($" 点击任务: {task.taskName}, 当前选中: {currentSelectedTaskId ?? "无"}");

            // 【新增】检查计时器是否正在运行，如果正在运行则不允许切换任务
            var timer = PomodoroTimerCore.Instance;
            if (timer != null && timer.CurrentState != TimerState.Idle)
            {
                Log(" 计时器正在运行中，不允许切换任务");

                // 显示提示
                ConfirmDialog.Instance?.ShowAlert(
                    Get("UI_Tasks", "task_cannot_switch"),
                    Get("UI_Tasks", "task_cannot_switch_msg")
                );
                return;
            }

            // 如果点击的是已选中的任务，则取消选择
            if (currentSelectedTaskId == task.id)
            {
                Log(" 取消选择任务");
                currentSelectedTaskId = null;
                mainUI?.OnTaskDeselected();

                // 更新所有任务项的选中状态为未选中
                foreach (var kvp in taskItemMap)
                {
                    kvp.Value.SetSelected(false);
                }
            }
            else
            {
                Log($" 选择新任务: {task.taskName}");
                // 选择新任务
                currentSelectedTaskId = task.id;
                mainUI?.OnTaskSelected(task);
                
                // 更新选中状态视觉
                foreach (var kvp in taskItemMap)
                {
                    kvp.Value.SetSelected(kvp.Key == task.id);
                }
            }
        }
        
        /// <summary>
        /// 任务项被编辑
        /// </summary>
        private void OnTaskItemEdit(TaskData task)
        {
            isEditMode = true;
            editingTask = task;
            selectedColorIndex = task.colorIndex;

            // 填充输入框
            if (taskNameInput != null)
            {
                taskNameInput.text = task.taskName;
            }

            // 显示删除按钮
            deleteTaskButton?.gameObject.SetActive(true);

            // 更新颜色选择
            UpdateColorSelection();

            // 【新增】计时器运行中时禁用颜色按钮
            bool timerBusy = IsTimerBusy();
            SetColorButtonsInteractable(!timerBusy);

            // 显示编辑面板
            taskEditPanel?.SetActive(true);
        }
        
        #endregion
        
        #region 任务管理器事件处理
        
        private void OnTaskAdded(TaskData task)
        {
            if (task != null && task.IsValid())
            {
                CreateTaskItem(task);
            }
        }
        
        private void OnTaskUpdated(TaskData task)
        {
            if (taskItemMap.TryGetValue(task.id, out var itemUI))
            {
                itemUI.UpdateDisplay(task);
            }
        }
        
        private void OnTaskDeleted(string taskId)
        {
            if (taskItemMap.TryGetValue(taskId, out var itemUI))
            {
                Destroy(itemUI.gameObject);
                taskItemMap.Remove(taskId);
            }
        }
        
        #endregion
        
        /// <summary>
        /// 更新颜色选择显示
        /// </summary>
        private void UpdateColorSelection()
        {
            for (int i = 0; i < colorButtons.Length; i++)
            {
                var btn = colorButtons[i];
                if (btn == null) continue;

                // 可以通过改变边框或缩放来显示选中状态
                var outline = btn.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.enabled = (i == selectedColorIndex);
                }

                // 或者改变缩放
                btn.transform.localScale = (i == selectedColorIndex)
                    ? Vector3.one * 1.2f
                    : Vector3.one;
            }
        }

        /// <summary>
        /// 检查计时器是否正在运行或暂停中（非空闲状态）
        /// </summary>
        private bool IsTimerBusy()
        {
            var timer = PomodoroTimerCore.Instance;
            return timer != null && timer.CurrentState != TimerState.Idle;
        }

        /// <summary>
        /// 设置颜色按钮的可交互状态
        /// </summary>
        private void SetColorButtonsInteractable(bool interactable)
        {
            for (int i = 0; i < colorButtons.Length; i++)
            {
                if (colorButtons[i] != null)
                {
                    colorButtons[i].interactable = interactable;
                }
            }
        }
    }
}
