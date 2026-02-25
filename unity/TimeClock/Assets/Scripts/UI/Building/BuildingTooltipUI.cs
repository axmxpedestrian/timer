using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using PomodoroTimer.Map.Data;
using PomodoroTimer.Resource;
using static PomodoroTimer.Utils.LocalizedText;

namespace PomodoroTimer.UI.Building
{
    /// <summary>
    /// 建筑物浮动信息面板
    /// 鼠标悬停在建筑项上时显示详细信息，跟随鼠标移动
    /// 支持延迟显示（防抖）和自动防出屏
    /// </summary>
    public class BuildingTooltipUI : MonoBehaviour
    {
        public static BuildingTooltipUI Instance { get; private set; }

        [Header("面板引用")]
        [SerializeField] private RectTransform tooltipPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("内容引用")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI categoryText;
        [SerializeField] private TextMeshProUGUI tagsText;

        [Header("设置")]
        [Tooltip("鼠标悬停多久后显示（秒）")]
        [SerializeField] private float showDelay = 0.35f;
        [Tooltip("面板相对于鼠标的偏移")]
        [SerializeField] private Vector2 offset = new Vector2(16f, -16f);
        [Tooltip("距离屏幕边缘的最小距离")]
        [SerializeField] private float screenEdgePadding = 8f;

        private Canvas rootCanvas;
        private RectTransform canvasRect;
        private BuildingItemData pendingData;
        private BuildingItemData currentData;
        private float hoverTimer;
        private bool isShowing;
        private bool isWaitingToShow;

        // BuildingCategory -> String Table key 映射
        private static readonly Dictionary<BuildingCategory, string> CategoryKeys =
            new Dictionary<BuildingCategory, string>
        {
            { BuildingCategory.All, "category_all" },
            { BuildingCategory.Residential, "category_residential" },
            { BuildingCategory.Road, "category_road" },
            { BuildingCategory.Nature, "category_nature" },
            { BuildingCategory.Facility, "category_facility" },
            { BuildingCategory.Structure, "category_structure" },
            { BuildingCategory.Decoration, "category_decoration" },
            { BuildingCategory.Commercial, "category_commercial" },
            { BuildingCategory.Industrial, "category_industrial" },
            { BuildingCategory.Infrastructure, "category_infrastructure" },
            { BuildingCategory.Special, "category_special" },
        };

        // 标签分类 key
        private static readonly string[] TagCategoryKeys = { "tag_type", "tag_style", "tag_cost", "tag_output" };

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null)
            {
                // 获取最顶层 Canvas
                Canvas[] canvases = rootCanvas.GetComponentsInParent<Canvas>();
                if (canvases.Length > 0)
                    rootCanvas = canvases[canvases.Length - 1];
                canvasRect = rootCanvas.GetComponent<RectTransform>();
            }

            // 运行时将 TooltipPanel 直接挂到 Canvas 下，确保坐标系一致且渲染在最上层
            if (tooltipPanel != null && canvasRect != null)
            {
                tooltipPanel.SetParent(canvasRect, false);
                tooltipPanel.SetAsLastSibling();
                // 强制 Pivot 为左上角
                tooltipPanel.pivot = new Vector2(0f, 1f);
                tooltipPanel.anchorMin = new Vector2(0.5f, 0.5f);
                tooltipPanel.anchorMax = new Vector2(0.5f, 0.5f);
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            // 延迟显示计时
            if (isWaitingToShow)
            {
                hoverTimer += Time.unscaledDeltaTime;
                if (hoverTimer >= showDelay)
                {
                    isWaitingToShow = false;
                    ShowTooltip(pendingData);
                }
            }

            // 跟随鼠标
            if (isShowing)
            {
                UpdatePosition();
            }
        }

        /// <summary>
        /// 请求显示 Tooltip（由 BuildingItemUI 调用）
        /// 会启动防抖计时器，延迟后才真正显示
        /// </summary>
        public void RequestShow(BuildingItemData data)
        {
            if (data == null) return;

            // 如果已经在显示同一个建筑的 Tooltip，不重复处理
            if (isShowing && currentData == data) return;

            // 如果正在等待显示同一个，不重置计时器
            if (isWaitingToShow && pendingData == data) return;

            pendingData = data;
            hoverTimer = 0f;
            isWaitingToShow = true;

            // 先隐藏之前可能显示的内容
            if (isShowing)
            {
                // 切换目标时立即更新内容（已经可见的情况下不需要再等延迟）
                isWaitingToShow = false;
                ShowTooltip(data);
            }
        }

        /// <summary>
        /// 请求隐藏 Tooltip（由 BuildingItemUI 调用）
        /// </summary>
        public void RequestHide(BuildingItemData data)
        {
            // 只隐藏对应的数据（防止多个 item 事件交叉）
            if (pendingData == data || currentData == data)
            {
                Hide();
            }
        }

        /// <summary>
        /// 强制隐藏
        /// </summary>
        public void ForceHide()
        {
            Hide();
        }

        private void ShowTooltip(BuildingItemData data)
        {
            if (data == null || tooltipPanel == null) return;

            currentData = data;
            isShowing = true;

            UpdateContent(data);
            UpdatePosition();

            tooltipPanel.gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false; // Tooltip 不拦截鼠标
            }
        }

        private void Hide()
        {
            isShowing = false;
            isWaitingToShow = false;
            pendingData = null;
            currentData = null;
            hoverTimer = 0f;

            if (tooltipPanel != null)
                tooltipPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// 更新 Tooltip 内容
        /// </summary>
        private void UpdateContent(BuildingItemData data)
        {
            var bp = data.blueprint;

            // 标题
            if (titleText != null)
                titleText.text = bp.buildingName;

            // 描述
            if (descriptionText != null)
            {
                descriptionText.text = string.IsNullOrEmpty(bp.description) ? "" : bp.description;
                descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(bp.description));
            }

            // 资源消耗
            if (costText != null)
            {
                costText.text = FormatCosts(bp.buildCosts);
                costText.gameObject.SetActive(bp.buildCosts != null && bp.buildCosts.Length > 0);
            }

            // 类别
            if (categoryText != null)
            {
                string catName;
                if (CategoryKeys.TryGetValue(bp.category, out string catKey))
                    catName = Get("UI_Building", catKey);
                else
                    catName = bp.category.ToString();
                categoryText.text = GetSmart("UI_Building", "tooltip_category", ("name", catName));
            }

            // 标签
            if (tagsText != null)
            {
                string tagStr = FormatTags(bp);
                tagsText.text = tagStr;
                tagsText.gameObject.SetActive(!string.IsNullOrEmpty(tagStr));
            }

            // 强制刷新布局，使 ContentSizeFitter 立即生效
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);
        }

        /// <summary>
        /// 格式化资源消耗列表
        /// </summary>
        private string FormatCosts(ResourceCost[] costs)
        {
            if (costs == null || costs.Length == 0) return "";

            var sb = new StringBuilder();
            sb.Append(Get("UI_Building", "tooltip_cost") + " ");
            for (int i = 0; i < costs.Length; i++)
            {
                if (i > 0) sb.Append("  ");

                var def = ResourceManager.Instance?.GetDefinition(costs[i].resourceType);
                string name = def != null ? def.resourceName : costs[i].resourceType.ToString();
                string amount = ResourceDefinition.FormatAmount(costs[i].amount);

                // 检查是否负担得起，用颜色标记
                bool canAfford = true;
                if (ResourceManager.Instance != null)
                    canAfford = ResourceManager.Instance.GetAmount(costs[i].resourceType) >= costs[i].amount;

                if (!canAfford)
                    sb.Append($"<color=#FF6666>{name} {amount}</color>");
                else
                    sb.Append($"{name} {amount}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 格式化标签信息
        /// </summary>
        private string FormatTags(BuildingBlueprint bp)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < BuildingTagFilter.CategoryCount; i++)
            {
                var tags = bp.GetTagsByCategory(i);
                if (tags == null || tags.Count == 0) continue;

                if (sb.Length > 0) sb.Append("  ");
                sb.Append($"{Get("UI_Building", TagCategoryKeys[i])}: ");
                for (int j = 0; j < tags.Count; j++)
                {
                    if (j > 0) sb.Append(", ");
                    sb.Append(tags[j]);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 更新面板位置，跟随鼠标并防止出屏
        /// </summary>
        private void UpdatePosition()
        {
            if (tooltipPanel == null || canvasRect == null) return;

            Vector2 mousePos = Input.mousePosition;

            // 将屏幕坐标转为 Canvas 局部坐标
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, mousePos, rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
                out localPoint);

            // 加上偏移
            localPoint += offset;

            // 获取面板尺寸和 Canvas 尺寸
            Vector2 panelSize = tooltipPanel.sizeDelta;
            Vector2 canvasSize = canvasRect.sizeDelta;
            Vector2 halfCanvas = canvasSize * 0.5f;

            // 防止右侧出屏：如果面板右边缘超出 Canvas 右边缘，翻到鼠标左侧
            if (localPoint.x + panelSize.x > halfCanvas.x - screenEdgePadding)
                localPoint.x = localPoint.x - panelSize.x - offset.x * 2f;

            // 防止下方出屏：如果面板下边缘超出 Canvas 下边缘，翻到鼠标上方
            if (localPoint.y - panelSize.y < -halfCanvas.y + screenEdgePadding)
                localPoint.y = localPoint.y + panelSize.y - offset.y * 2f;

            // 防止左侧出屏
            if (localPoint.x < -halfCanvas.x + screenEdgePadding)
                localPoint.x = -halfCanvas.x + screenEdgePadding;

            // 防止上方出屏
            if (localPoint.y > halfCanvas.y - screenEdgePadding)
                localPoint.y = halfCanvas.y - screenEdgePadding;

            tooltipPanel.anchoredPosition = localPoint;
        }
    }
}
