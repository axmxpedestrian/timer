using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PomodoroTimer.Data;
using PomodoroTimer.Utils;

namespace PomodoroTimer.UI
{
    /// <summary>
    /// 单个任务项UI
    /// </summary>
    public class TaskItemUI : MonoBehaviour
    {
        [Header("UI元素")]
        [SerializeField] private Image colorBar;
        [SerializeField] private TextMeshProUGUI taskNameText;
        [SerializeField] private TextMeshProUGUI pomodoroCountText;
        [SerializeField] private TextMeshProUGUI totalTimeText;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button editButton;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject selectedIndicator;
        
        private TaskData taskData;
        private Action<TaskData> onSelectCallback;
        private Action<TaskData> onEditCallback;
        private bool isSelected = false;
        
        /// <summary>
        /// 设置任务项
        /// </summary>
        public void Setup(TaskData task, Action<TaskData> onSelect, Action<TaskData> onEdit)
        {
            taskData = task;
            onSelectCallback = onSelect;
            onEditCallback = onEdit;
            
            // 绑定按钮
            selectButton?.onClick.AddListener(OnSelectClicked);
            editButton?.onClick.AddListener(OnEditClicked);
            
            // 【修复】确保装饰性Image不阻挡点击事件
            DisableRaycastOnDecorators();
            
            // 更新显示
            UpdateDisplay(task);
        }
        
        /// <summary>
        /// 禁用装饰性元素的Raycast，防止阻挡按钮点击
        /// </summary>
        private void DisableRaycastOnDecorators()
        {
            // 选中指示器不应该阻挡点击
            if (selectedIndicator != null)
            {
                var image = selectedIndicator.GetComponent<Image>();
                if (image != null)
                {
                    image.raycastTarget = false;
                }
                // 也检查子物体
                foreach (var img in selectedIndicator.GetComponentsInChildren<Image>())
                {
                    img.raycastTarget = false;
                }
            }
            
            // 背景图片如果不是按钮本身，也不应该阻挡
            // 注意：如果backgroundImage就是selectButton的Image，则不要禁用
            if (backgroundImage != null && selectButton != null)
            {
                var buttonImage = selectButton.GetComponent<Image>();
                if (backgroundImage != buttonImage)
                {
                    backgroundImage.raycastTarget = false;
                }
            }
            
            // 颜色条也不应该阻挡
            if (colorBar != null)
            {
                colorBar.raycastTarget = false;
            }
        }
        
        /// <summary>
        /// 更新显示
        /// </summary>
        public void UpdateDisplay(TaskData task)
        {
            taskData = task;
            
            // 更新颜色条
            if (colorBar != null)
            {
                colorBar.color = ColorPalette.GetTaskColor(task.colorIndex);
            }
            
            // 更新任务名
            if (taskNameText != null)
            {
                taskNameText.text = task.taskName;
                
                // 如果已完成，添加删除线效果
                if (task.isCompleted)
                {
                    taskNameText.fontStyle = FontStyles.Strikethrough;
                    taskNameText.color = ColorPalette.Theme.TextSecondary;
                }
                else
                {
                    taskNameText.fontStyle = FontStyles.Normal;
                    taskNameText.color = ColorPalette.Theme.TextPrimary;
                }
            }
            
            // 更新番茄钟计数
            if (pomodoroCountText != null)
            {
                pomodoroCountText.text = $"🍅 {task.completedPomodoros}";
            }
            
            // 更新总时间
            if (totalTimeText != null)
            {
                totalTimeText.text = task.GetFormattedTotalTime();
            }
        }
        
        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            // 更新选中指示器
            selectedIndicator?.SetActive(selected);
            
            // 更新背景色
            if (backgroundImage != null)
            {
                backgroundImage.color = selected 
                    ? ColorPalette.GetTransparent(ColorPalette.GetTaskColor(taskData.colorIndex), 0.1f)
                    : Color.white;
            }
        }
        
        private void OnSelectClicked()
        {
            AudioManager.Instance?.PlayClick();
            onSelectCallback?.Invoke(taskData);
        }
        
        private void OnEditClicked()
        {
            AudioManager.Instance?.PlayClick();
            onEditCallback?.Invoke(taskData);
        }
        
        private void OnDestroy()
        {
            selectButton?.onClick.RemoveAllListeners();
            editButton?.onClick.RemoveAllListeners();
        }
    }
}
