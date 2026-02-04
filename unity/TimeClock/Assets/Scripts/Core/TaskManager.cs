using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PomodoroTimer.Data;

namespace PomodoroTimer.Core
{
    /// <summary>
    /// 任务管理器
    /// </summary>
    public class TaskManager : MonoBehaviour
    {
        public static TaskManager Instance { get; private set; }
        
        private List<TaskData> tasks = new List<TaskData>();
        
        // 事件
        public event Action<TaskData> OnTaskAdded;
        public event Action<TaskData> OnTaskUpdated;
        public event Action<string> OnTaskDeleted;
        public event Action OnTaskListChanged;
        
        public IReadOnlyList<TaskData> Tasks => tasks.AsReadOnly();
        
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
        
        /// <summary>
        /// 初始化任务列表(从存档加载)
        /// </summary>
        public void Initialize(List<TaskData> savedTasks)
        {
            tasks = savedTasks ?? new List<TaskData>();
            OnTaskListChanged?.Invoke();
        }
        
        /// <summary>
        /// 创建新任务
        /// </summary>
        public TaskData CreateTask(string name, int colorIndex = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Debug.LogWarning("任务名称不能为空");
                return null;
            }
            
            var task = new TaskData(name, colorIndex);
            tasks.Add(task);
            
            DataManager.Instance.Save();
            
            OnTaskAdded?.Invoke(task);
            OnTaskListChanged?.Invoke();
            
            return task;
        }
        
        /// <summary>
        /// 更新任务
        /// </summary>
        public void UpdateTask(string taskId, string newName, int newColorIndex)
        {
            var task = GetTaskById(taskId);
            if (task == null)
            {
                Debug.LogWarning($"找不到任务: {taskId}");
                return;
            }
            
            task.taskName = newName;
            task.colorIndex = Mathf.Clamp(newColorIndex, 0, 5);
            
            DataManager.Instance.Save();
            
            OnTaskUpdated?.Invoke(task);
            OnTaskListChanged?.Invoke();
        }
        
        /// <summary>
        /// 删除任务
        /// </summary>
        public void DeleteTask(string taskId)
        {
            var task = GetTaskById(taskId);
            if (task == null)
            {
                Debug.LogWarning($"找不到任务: {taskId}");
                return;
            }
            
            tasks.Remove(task);
            
            DataManager.Instance.Save();
            
            OnTaskDeleted?.Invoke(taskId);
            OnTaskListChanged?.Invoke();
        }
        
        /// <summary>
        /// 标记任务完成
        /// </summary>
        public void MarkTaskComplete(string taskId, bool completed = true)
        {
            var task = GetTaskById(taskId);
            if (task == null) return;
            
            task.isCompleted = completed;
            
            DataManager.Instance.Save();
            
            OnTaskUpdated?.Invoke(task);
        }
        
        /// <summary>
        /// 添加番茄钟到任务
        /// </summary>
        public void AddPomodoroToTask(string taskId, float focusTimeSeconds)
        {
            var task = GetTaskById(taskId);
            if (task == null) return;
            
            task.AddCompletedPomodoro(focusTimeSeconds);
            
            DataManager.Instance.Save();
            
            OnTaskUpdated?.Invoke(task);
        }
        
        /// <summary>
        /// 根据ID获取任务
        /// </summary>
        public TaskData GetTaskById(string taskId)
        {
            return tasks.FirstOrDefault(t => t.id == taskId);
        }
        
        /// <summary>
        /// 获取活动任务(未完成)
        /// </summary>
        public List<TaskData> GetActiveTasks()
        {
            return tasks.Where(t => !t.isCompleted).ToList();
        }
        
        /// <summary>
        /// 获取已完成任务
        /// </summary>
        public List<TaskData> GetCompletedTasks()
        {
            return tasks.Where(t => t.isCompleted).ToList();
        }
        
        /// <summary>
        /// 按番茄钟数量排序获取任务
        /// </summary>
        public List<TaskData> GetTasksSortedByPomodoros(bool descending = true)
        {
            return descending 
                ? tasks.OrderByDescending(t => t.completedPomodoros).ToList()
                : tasks.OrderBy(t => t.completedPomodoros).ToList();
        }
        
        /// <summary>
        /// 获取今日活跃的任务
        /// </summary>
        public List<TaskData> GetTodayActiveTasks()
        {
            var todayRecords = StatisticsManager.Instance.GetTodayRecords();
            var taskIds = todayRecords.Select(r => r.taskId).Distinct();
            return tasks.Where(t => taskIds.Contains(t.id)).ToList();
        }
        
        /// <summary>
        /// 清除所有已完成任务
        /// </summary>
        public void ClearCompletedTasks()
        {
            var completedIds = tasks.Where(t => t.isCompleted).Select(t => t.id).ToList();
            tasks.RemoveAll(t => t.isCompleted);
            
            DataManager.Instance.Save();
            
            foreach (var id in completedIds)
            {
                OnTaskDeleted?.Invoke(id);
            }
            OnTaskListChanged?.Invoke();
        }
        
        /// <summary>
        /// 获取任务总数
        /// </summary>
        public int GetTotalTaskCount()
        {
            return tasks.Count;
        }
        
        /// <summary>
        /// 获取活动任务数量
        /// </summary>
        public int GetActiveTaskCount()
        {
            return tasks.Count(t => !t.isCompleted);
        }
        
        /// <summary>
        /// 重置指定任务的统计数据
        /// </summary>
        public void ResetTaskStatistics(string taskId)
        {
            var task = GetTaskById(taskId);
            if (task != null)
            {
                task.completedPomodoros = 0;
                task.totalFocusTimeSeconds = 0;
                OnTaskUpdated?.Invoke(task);
            }
        }
        
        /// <summary>
        /// 从记录中重新计算任务统计数据
        /// </summary>
        public void RecalculateTaskStatistics(string taskId)
        {
            var task = GetTaskById(taskId);
            if (task == null) return;
            
            var records = StatisticsManager.Instance?.GetAllRecords();
            if (records == null) return;
            
            var taskRecords = records.Where(r => 
                r.taskId == taskId && 
                r.pomodoroType == PomodoroType.Focus
            ).ToList();
            
            task.completedPomodoros = taskRecords.Count;
            task.totalFocusTimeSeconds = taskRecords.Sum(r => r.durationSeconds);
            
            OnTaskUpdated?.Invoke(task);
        }
    }
}
