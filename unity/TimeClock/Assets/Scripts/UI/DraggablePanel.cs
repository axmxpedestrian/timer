using UnityEngine;
using UnityEngine.EventSystems;

namespace PomodoroTimer.UI
{
    /// <summary>
    /// 可拖动面板组件
    /// 挂载到需要拖动的UI元素上（如TimerBackground）
    /// 拖动时会移动指定的目标面板
    /// </summary>
    public class DraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [Header("设置")]
        [SerializeField] private RectTransform targetPanel;      // 要移动的目标面板（如TimerSection）
        [SerializeField] private bool clampToScreen = true;      // 是否限制在屏幕范围内
        [SerializeField] private float edgePadding = 10f;        // 边缘留白

        [Header("调试")]
        [SerializeField] private bool enableDebugLog = false;

        private Canvas parentCanvas;
        private RectTransform canvasRectTransform;
        private Vector2 dragOffset;
        private Vector2 defaultPosition;
        private bool hasStoredDefaultPosition = false;
        private bool isDragging = false;

        private void Awake()
        {
            // 查找父Canvas
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                canvasRectTransform = parentCanvas.GetComponent<RectTransform>();
            }

            // 如果没有指定目标面板，使用自身
            if (targetPanel == null)
            {
                targetPanel = GetComponent<RectTransform>();
            }
        }

        private void Start()
        {
            // 存储默认位置
            StoreDefaultPosition();
        }

        /// <summary>
        /// 存储默认位置
        /// </summary>
        private void StoreDefaultPosition()
        {
            if (targetPanel != null && !hasStoredDefaultPosition)
            {
                defaultPosition = targetPanel.anchoredPosition;
                hasStoredDefaultPosition = true;
                Log($"存储默认位置: {defaultPosition}");
            }
        }

        /// <summary>
        /// 重置到默认位置
        /// </summary>
        public void ResetToDefaultPosition()
        {
            if (targetPanel != null && hasStoredDefaultPosition)
            {
                targetPanel.anchoredPosition = defaultPosition;
                Log($"重置到默认位置: {defaultPosition}");
            }
        }

        /// <summary>
        /// 设置目标面板
        /// </summary>
        public void SetTargetPanel(RectTransform target)
        {
            targetPanel = target;
            hasStoredDefaultPosition = false;
            StoreDefaultPosition();
        }

        #region 拖动事件处理

        public void OnPointerDown(PointerEventData eventData)
        {
            // 确保存储了默认位置
            StoreDefaultPosition();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (targetPanel == null) return;

            isDragging = true;

            // 计算鼠标点击位置与面板位置的偏移
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint))
            {
                dragOffset = targetPanel.anchoredPosition - localPoint;
            }

            Log($"开始拖动，偏移: {dragOffset}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (targetPanel == null || !isDragging) return;

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint))
            {
                Vector2 newPosition = localPoint + dragOffset;

                // 限制在屏幕范围内
                if (clampToScreen)
                {
                    newPosition = ClampToScreen(newPosition);
                }

                targetPanel.anchoredPosition = newPosition;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            Log($"结束拖动，最终位置: {targetPanel.anchoredPosition}");
        }

        #endregion

        /// <summary>
        /// 将位置限制在屏幕范围内
        /// </summary>
        private Vector2 ClampToScreen(Vector2 position)
        {
            if (targetPanel == null || canvasRectTransform == null) return position;

            // 获取面板尺寸
            Vector2 panelSize = targetPanel.rect.size;
            Vector2 pivot = targetPanel.pivot;

            // 获取Canvas尺寸
            Vector2 canvasSize = canvasRectTransform.rect.size;

            // 计算面板在当前锚点设置下的边界
            // 考虑pivot的影响
            float leftOffset = panelSize.x * pivot.x;
            float rightOffset = panelSize.x * (1 - pivot.x);
            float bottomOffset = panelSize.y * pivot.y;
            float topOffset = panelSize.y * (1 - pivot.y);

            // 获取锚点位置（相对于Canvas中心的偏移）
            Vector2 anchorMin = targetPanel.anchorMin;
            Vector2 anchorMax = targetPanel.anchorMax;
            Vector2 anchorCenter = (anchorMin + anchorMax) / 2f;

            // 计算锚点相对于Canvas中心的偏移
            Vector2 anchorOffset = new Vector2(
                (anchorCenter.x - 0.5f) * canvasSize.x,
                (anchorCenter.y - 0.5f) * canvasSize.y
            );

            // 计算允许的位置范围
            float minX = -canvasSize.x / 2f + leftOffset + edgePadding - anchorOffset.x;
            float maxX = canvasSize.x / 2f - rightOffset - edgePadding - anchorOffset.x;
            float minY = -canvasSize.y / 2f + bottomOffset + edgePadding - anchorOffset.y;
            float maxY = canvasSize.y / 2f - topOffset - edgePadding - anchorOffset.y;

            // 限制位置
            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);

            return position;
        }

        /// <summary>
        /// 条件日志输出
        /// </summary>
        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[DraggablePanel] {message}");
            }
        }

        #region 编辑器辅助

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 在编辑器中自动查找目标面板
            if (targetPanel == null)
            {
                // 尝试查找父级的TimerSection
                Transform parent = transform.parent;
                while (parent != null)
                {
                    if (parent.name.Contains("TimerSection") || parent.name.Contains("Timer"))
                    {
                        targetPanel = parent.GetComponent<RectTransform>();
                        break;
                    }
                    parent = parent.parent;
                }
            }
        }
#endif

        #endregion
    }
}
