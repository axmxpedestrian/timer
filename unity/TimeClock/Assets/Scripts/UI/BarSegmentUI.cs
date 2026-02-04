using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PomodoroTimer.UI
{
    /// <summary>
    /// 柱状图段UI - 处理鼠标悬停显示提示
    /// </summary>
    public class BarSegmentUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string taskName;
        private float minutes;
        private Color originalColor;
        private Image image;
        
        [Header("高亮设置")]
        [SerializeField] private float highlightBrightness = 0.2f;
        
        // 回调
        private System.Action<BarSegmentUI> onPointerEnter;
        private System.Action<BarSegmentUI> onPointerExit;
        
        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName => taskName;
        
        /// <summary>
        /// 专注时长（分钟）
        /// </summary>
        public float Minutes => minutes;
        
        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(string taskName, float minutes, 
                               System.Action<BarSegmentUI> onEnter, 
                               System.Action<BarSegmentUI> onExit)
        {
            this.taskName = taskName;
            this.minutes = minutes;
            this.onPointerEnter = onEnter;
            this.onPointerExit = onExit;
            
            image = GetComponent<Image>();
            if (image != null)
            {
                originalColor = image.color;
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 高亮显示
            if (image != null)
            {
                image.color = Brighten(originalColor, highlightBrightness);
            }
            
            onPointerEnter?.Invoke(this);
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            // 恢复原色
            if (image != null)
            {
                image.color = originalColor;
            }
            
            onPointerExit?.Invoke(this);
        }
        
        /// <summary>
        /// 提亮颜色
        /// </summary>
        private Color Brighten(Color color, float amount)
        {
            return new Color(
                Mathf.Min(1f, color.r + amount),
                Mathf.Min(1f, color.g + amount),
                Mathf.Min(1f, color.b + amount),
                color.a
            );
        }
        
        /// <summary>
        /// 更新原始颜色（颜色变化时调用）
        /// </summary>
        public void UpdateOriginalColor(Color newColor)
        {
            originalColor = newColor;
            if (image != null)
            {
                image.color = newColor;
            }
        }
    }
}
